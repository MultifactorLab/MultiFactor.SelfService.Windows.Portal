using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using System;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi;
using System.Linq;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;
using System.Web.UI.WebControls;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Core;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignIn
{
    public class IdentityStory
    {
        private readonly IMultiFactorApi _multifactorApiClient;
        private readonly IMultifactorIdpApi _idpApiClient;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly Configuration _settings;
        private readonly ILogger _logger;
        private readonly ClaimsProvider _claimsProvider;
        private readonly ICredentialVerifier _credentialVerifier;
        private readonly AuthnStory _authnStory;
        private readonly IApplicationCache _applicationCache;

        public IdentityStory(
            IMultiFactorApi multifactorApiClient,
            IMultifactorIdpApi idpApiClient,
            SafeHttpContextAccessor contextAccessor,
            Configuration settings,
            ILogger logger,
            ClaimsProvider claimsProvider,
            ICredentialVerifier credentialVerifier,
            AuthnStory authnStory,
            IApplicationCache applicationCache)
        {
            _multifactorApiClient = multifactorApiClient;
            _idpApiClient = idpApiClient;
            _contextAccessor = contextAccessor;
            _settings = settings;
            _logger = logger;
            _claimsProvider = claimsProvider;
            _credentialVerifier = credentialVerifier;
            _authnStory = authnStory;
            _applicationCache = applicationCache;
        }

        public async Task<ActionResult> ExecuteAsync(IdentityModel model, Dictionary<string, string> headers)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            var username = model.UserName.Trim();

            // Validate username format if UPN is required
            if (_settings.RequiresUpn && !IsUserPrincipalName(username))
            {
                _logger.Warning("UPN format required but not provided for user input");
                throw new ModelStateErrorException("WrongUserNameOrPassword");
            }

            VerifiedMembershipDto verifiedMembership = null;
            var verifiedUsername = username;
            // Verify membership locally if prebind info is needed
            if (_settings.NeedPrebindInfo())
            {
                _logger.Debug("Verifying membership locally for user '{User}'", username);
                var membershipResult = await _credentialVerifier.VerifyMembership(username);
                verifiedUsername = membershipResult.Username;

                if (!membershipResult.IsAuthenticated)
                {
                    _logger.Warning("Membership verification failed for user '{User}': {Reason}", username, membershipResult.Reason);
                    throw new ModelStateErrorException("WrongUserNameOrPassword");
                }

                _logger.Information("User '{User}' membership verified successfully", username);
                verifiedMembership = MapToVerifiedMembershipDto(membershipResult);
            }

            var authenticators = await _multifactorApiClient.GetUserAuthenticatorsAsync(username);
            if (!authenticators.GetAuthenticators().Any())
            {
                return new ViewResult
                {
                    ViewName = "Login",
                    ViewData = new ViewDataDictionary
                    {
                        Model = new LoginModel()
                        {
                            UserName = username
                        }

                    }
                };
            }

            var claims = _claimsProvider.GetClaims().ToDictionary(kv => kv.Key, kv => kv.Value);
            claims[AuthenticationClaims.AUTHENTICATION_METHODS_REFERENCES] = AuthenticationClaims.PASSWORD_METHOD;

            var sso = _contextAccessor.SafeGetSsoClaims();
            var postbackUrl = model.MyUrl.BuildPostbackUrl();

            var request = new IdentityRequestDto
            {
                Username = verifiedUsername,
                VerifiedMembership = verifiedMembership,
                SamlSessionId = sso.SamlSessionId,
                OidcSessionId = sso.OidcSessionId,
                LoginCompletedCallbackUrl = postbackUrl,
                AdditionalClaims = claims.ToDictionary(x => x.Key, x => x.Value),
                Settings = BuildSspSettings()
            };

            var response = await _idpApiClient.IdentityAsync(request, headers);
            return await HandleIdentityResponse(response, model, verifiedUsername);
        }

        private IdentitySspSettingsDto BuildSspSettings()
        {
            return new IdentitySspSettingsDto
            {
                PreAuthenticationMethod = _settings.PreAuthnMode,
                RequiresUserPrincipalName = _settings.RequiresUpn,
                NeedPrebindInfo = _settings.NeedPrebindInfo(),
                UseUpnAsIdentity = _settings.UseUpnAsIdentity,
                NetBiosName = _settings.NetBiosName,
                SignUpGroups = _settings.SignUpGroups
            };
        }

        private async Task<ActionResult> HandleIdentityResponse(IdentityResponseDto response, IdentityModel model, string verifiedUsername)
        {
            if (response.Action == IdentityAction.AccessDenied)
            {
                _logger.Warning("Access denied for user '{User}'", model.UserName);
                return new RedirectToActionResult().ToActionResult("AccessDenied", "Error", null);
            }

            if (!response.Success)
            {
                _logger.Debug("Identity verification failed: {Error}", response.ErrorMessage);
                throw new ModelStateErrorException("WrongUserNameOrPassword");
            }

            if (response.Action == IdentityAction.MfaRequired && !string.IsNullOrWhiteSpace(response.RedirectUrl))
            {
                _applicationCache.SetPreauthenticationIdentity(
                    ApplicationCacheKeyFactory.CreatePreAuthenticationIdentityKey(verifiedUsername),
                    model);

                _logger.Debug("Redirecting user '{User}' to MFA page", model.UserName);
                return new RedirectResult(response.RedirectUrl, true);
            }

            if (response.Action == IdentityAction.ShowAuthn)
            {
                var identity = response.Username ?? model.UserName;
                _logger.Information("Bypass second factor for user '{User}', showing password form", identity);

                try
                {
                    return await _authnStory.ExecuteAsync(model);
                }
                catch (ModelStateErrorException ex)
                {
                    var viewData = new ViewDataDictionary
                    {
                        Model = new IdentityModel
                        {
                            UserName = identity,
                            Password = model.Password,
                            MyUrl = model.MyUrl,
                            AccessToken = model.AccessToken
                        }
                    };
                    viewData.ModelState.AddModelError(string.Empty, ex.Message);

                    return new ViewResult
                    {
                        ViewName = "Authn",
                        ViewData = viewData
                    };
                }

            }

            if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
            {
                return new RedirectResult(response.RedirectUrl, true);
            }

            throw new ModelStateErrorException("WrongUserNameOrPassword");
        }

        private static VerifiedMembershipDto MapToVerifiedMembershipDto(CredentialVerificationResult result)
        {
            return new VerifiedMembershipDto
            {
                IsBypass = result.IsBypass,
                DisplayName = result.DisplayName,
                Email = result.Email,
                Phone = result.Phone,
                UserPrincipalName = result.UserPrincipalName,
                CustomIdentity = result.CustomIdentity
            };
        }

        private static bool IsUserPrincipalName(string username)
        {
            return username.Contains('@');
        }
    }
}