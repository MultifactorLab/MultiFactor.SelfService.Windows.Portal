using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using System;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Windows.Portal;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using System.Linq;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Stories;
using System.Web.UI.WebControls;
using MultiFactor.SelfService.Windows.Portal.Models;

public class SignInStory
{
    private readonly IMultifactorIdpApi _idpApiClient;
    private readonly IMultiFactorApi _apiClient;
    private readonly DataProtection _dataProtection;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly Configuration _settings;
    private readonly ILogger _logger;
    private readonly IApplicationCache _applicationCache;
    private readonly ClaimsProvider _claimsProvider;
    private readonly ICredentialVerifier _credentialVerifier;

    public SignInStory(
        IMultifactorIdpApi idpApiClient,
        IMultiFactorApi apiClient,
        DataProtection dataProtection,
        SafeHttpContextAccessor contextAccessor,
        Configuration settings,
        IApplicationCache applicationCache,
        ILogger logger,
        ClaimsProvider claimsProvider,
        ICredentialVerifier credentialVerifier
        )
    {
        _idpApiClient = idpApiClient;
        _apiClient = apiClient;
        _dataProtection = dataProtection;
        _contextAccessor = contextAccessor;
        _settings = settings;
        _logger = logger;
        _applicationCache = applicationCache;
        _claimsProvider = claimsProvider;
        _credentialVerifier = credentialVerifier;
    }

    public async Task<ActionResult> ExecuteAsync(LoginModel model, Dictionary<string, string> headers)
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
        var password = model.Password.Trim();


        if (_settings.RequiresUpn && !IsUserPrincipalName(username))
        {
            _logger.Warning("UPN format required but not provided for user input");
            throw new ModelStateErrorException("WrongUserNameOrPassword");
        }

        var userName = LdapIdentity.ParseUser(username);
        _logger.Debug("Verifying credentials locally for user '{User}'", username);
        var credentialResult = await _credentialVerifier.VerifyCredentialAsync(username, password);

        if (!credentialResult.IsAuthenticated && !credentialResult.UserMustChangePassword)
        {
            _logger.Warning("Credential verification failed for user '{User}': {Reason}", username, credentialResult.Reason);
            await DelayedFailureAsync();
            throw new ModelStateErrorException("WrongUserNameOrPassword");
        }

        _logger.Information("User '{User}' credentials verified successfully", username);

        var claims = _claimsProvider.GetClaims().ToDictionary(x => x.Key, x => x.Value);
        claims.Add(AuthenticationClaims.AUTHENTICATION_METHODS_REFERENCES, AuthenticationClaims.PASSWORD_METHOD);

        var sso = _contextAccessor.SafeGetSsoClaims();
        var postbackUrl = model.MyUrl.BuildPostbackUrl();

        var request = new LoginRequestDto
        {
            VerifiedCredentials = MapToVerifiedCredentialsDto(credentialResult),
            SamlSessionId = sso.SamlSessionId,
            OidcSessionId = sso.OidcSessionId,
            LoginCompletedCallbackUrl = null,
            AdditionalClaims = claims.ToDictionary(x => x.Key, x => x.Value),
            Settings = BuildSspSettings()
        };

        var response = await _idpApiClient.LoginAsync(request, headers);

        return await HandleLoginResponse(response, model, credentialResult);
    }

    private SspSettingsDto BuildSspSettings()
    {
        return new SspSettingsDto
        {
            PreAuthenticationMethod = _settings.PreAuthnMode,
            RequiresUserPrincipalName = _settings.RequiresUpn,
            PasswordManagementEnabled = _settings.EnablePasswordManagement,
            NeedPrebindInfo = _settings.NeedPrebindInfo(),
            NetBiosName = _settings.NetBiosName,
            SignUpGroups = _settings.SignUpGroups
        };
    }

    private async Task<ActionResult> HandleLoginResponse(LoginResponseDto response, LoginModel model, CredentialVerificationResult adValidationResult)
    {
        if (response.Action == LoginAction.AccessDenied)
        {
            _logger.Warning("Access denied for user '{User}'", model.UserName);
            return new RedirectToActionResult().ToActionResult("AccessDenied", "Error", null);
        }

        if (!response.Success)
        {
            _logger.Debug("Login failed: {Error}", response.ErrorMessage);
            throw new ModelStateErrorException("WrongUserNameOrPassword");
        }

        if (response.Action == LoginAction.MfaRequired && !string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            if (_settings.PreAuthnMode)
            {
                _applicationCache.SetPreauthenticationAuthn(
                    ApplicationCacheKeyFactory.CreatePreAuthenticationAuthnSucceedKey(adValidationResult.Username),
                    true);
            }

            _logger.Debug("Redirecting user to MFA page");
            return new RedirectResult(response.RedirectUrl, true);
        }

        if (response.Action == LoginAction.BypassSaml)
        {
            _logger.Debug("Bypass second factor for user '{User}' via SAML", model.UserName);

            var userIdentity = GetIdentity(adValidationResult);

            var sso = _contextAccessor.SafeGetSsoClaims();
            var user = new UserProfileDto(string.Empty, userIdentity)
            {
                Email = adValidationResult.Email,
                Name = adValidationResult.DisplayName,
            };

            var page = await _apiClient.CreateSamlBypassRequestAsync(user, sso.SamlSessionId);
            return new RedirectToActionResult().ToActionResult("ByPassSsoSession", "Account",
                new { callbackUrl = page.CallbackUrl, accessToken = page.AccessToken });
        }

        if (response.Action == LoginAction.BypassOidc)
        {
            _logger.Debug("Bypass second factor for user '{User}' via OIDC", model.UserName);
            var sso = _contextAccessor.SafeGetSsoClaims();
            return new RedirectToActionResult().ToActionResult("ByPassOidcSession", "Account",
                new { oidcSession = sso.OidcSessionId });
        }

        if (response.Action == LoginAction.ChangePassword)
        {
            _logger.Information("User '{User}' must change password", model.UserName);

            var encryptedPassword = _dataProtection.Protect(
                model.Password.Trim(),
                Constants.PWD_RENEWAL_PURPOSE);
            _applicationCache.Set(
                ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName),
                model.UserName.Trim());
            _applicationCache.Set(
                ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName),
                encryptedPassword);

            if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
            {
                return new RedirectResult(response.RedirectUrl, true);
            }

            return new RedirectToActionResult().ToActionResult("Change", "ExpiredPassword", null);
        }

        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return new RedirectResult(response.RedirectUrl, true);
        }

        throw new ModelStateErrorException("WrongUserNameOrPassword");
    }

    private static VerifiedCredentialsDto MapToVerifiedCredentialsDto(CredentialVerificationResult result)
    {
        return new VerifiedCredentialsDto
        {
            IsAuthenticated = result.IsAuthenticated,
            IsBypass = result.IsBypass,
            UserMustChangePassword = result.UserMustChangePassword,
            PasswordExpirationDate = result.PasswordExpirationDate,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Phone = result.Phone,
            Username = result.Username,
            UserPrincipalName = result.UserPrincipalName,
            CustomIdentity = result.CustomIdentity,
            Reason = result.Reason
        };
    }

    private static bool IsUserPrincipalName(string username)
    {
        return username.Contains('@');
    }

    private static async Task DelayedFailureAsync()
    {
        var rnd = new Random();
        var delay = rnd.Next(2, 6);
        await Task.Delay(TimeSpan.FromSeconds(delay));
    }

    private string GetIdentity(CredentialVerificationResult verificationResult)
    {
        return !string.IsNullOrWhiteSpace(verificationResult.CustomIdentity)
            ? verificationResult.CustomIdentity
            : verificationResult.Username;
    }
}
