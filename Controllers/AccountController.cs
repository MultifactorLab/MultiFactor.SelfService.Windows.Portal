using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Windows.Portal.Stories.SignOut;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Stories;
using MultiFactor.SelfService.Windows.Portal.Stories.LoadProfile;
using MultiFactor.SelfService.Windows.Portal.Stories.SignIn;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class AccountController : ControllerBase
    {
        private readonly ApplicationCache _applicationCache;
        private readonly MultiFactorApiClient _apiClient;
        private readonly ILogger _logger;
        private readonly IMultifactorIdpApi _multifactorIdpApi;
        private readonly LoadProfileStory _loadProfileStory;
        private readonly SignInStory _signInStory;
        private readonly IdentityStory _identityStory;
        private readonly AuthnStory _authnStory;
        private readonly SignOutStory _signOutStory;
        private readonly AuthenticateSessionStory _authenticateSessionStory;
        private readonly RedirectToCredValidationAfter2FaStory _redirectToCredValidationAfter2FaStory;
        private const string CallbackFromMfa = "PostbackFromMfa";

        public AccountController(ApplicationCache applicationCache,
            AuthService authService,
            MultiFactorApiClient apiClient,
            LoadProfileStory loadProfileStory,
            SignInStory signInStory,
            IdentityStory identityStory,
            AuthnStory AuthnStory,
            SignOutStory signOutStory,
            AuthenticateSessionStory authenticateSessionStory,
            RedirectToCredValidationAfter2FaStory redirectToCredValidationAfter2FaStory,
            ILogger logger)
        {
            _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _loadProfileStory = loadProfileStory;
            _signInStory = signInStory;
            _identityStory = identityStory;
            _authnStory = AuthnStory;
            _signOutStory = signOutStory;
            _authenticateSessionStory = authenticateSessionStory;
            _redirectToCredValidationAfter2FaStory = redirectToCredValidationAfter2FaStory;
        }

        [HttpGet]
        public ActionResult Login(SingleSignOnDto sso)
        {
            if (Configuration.Current.PreAuthnMode)
            {
                return RedirectToAction("Identity", sso);
            }

            bool userAuthenticated = Request.IsAuthenticated;
            //integrated windows authentication
            bool authenticateWindowsUser =
                Configuration.AuthenticationMode == AuthenticationMode.Windows && User.Identity != null;
            bool negotiateAuthentication = !string.IsNullOrEmpty(User.Identity?.Name) &&
                                           User.Identity.AuthenticationType == "Negotiate";

            if (!userAuthenticated || !authenticateWindowsUser || !negotiateAuthentication)
            {
                return View(new LoginModel());
            }

            var userName = User.Identity.Name;

            _logger.Information("User '{user:l}' authenticated by NTLM/Kerberos", userName);
            return RedirectToMfa(
                identity: userName,
                login: userName,
                documentUrl: Request?.Url?.ToString(),
                samlSessionId: sso.SamlSessionId,
                oidcSessionId: sso.OidcSessionId
            );
        }

        [HttpPost]
        [VerifyCaptcha]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginModel model, SingleSignOnDto sso)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var headers = HttpContext.GetRequiredHeaders();

                return await _signInStory.ExecuteAsync(model, headers);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// 2-step user verification: 2fa then AD credentials (first factor)
        /// </summary>
        /// <param name="sso">Model for sso integration. Can be empty.</param>
        /// <param name="requestId">State for continuation user verification.</param>
        /// <returns></returns>
        public async Task<ActionResult> Identity(SingleSignOnDto sso, string requestId)
        {
            if (!Configuration.Current.PreAuthnMode)
            {
                return RedirectToAction("Login");
            }

            try
            {
                await _loadProfileStory.ExecuteAsync();

                if (sso.HasSamlSession())
                {
                    return new RedirectToActionResult().ToActionResult("ByPassSamlSession", "Account",
                        new { samlSession = sso.SamlSessionId });
                }

                if (sso.HasOidcSession())
                {
                    return new RedirectToActionResult().ToActionResult("ByPassOidcSession", "Account",
                        new { oidcSession = sso.OidcSessionId });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (UnauthorizedException ex)
            {
                if (!Configuration.Current.PreAuthnMode)
                {
                    return RedirectToAction("Login", sso.ToString());
                }

                var identity = _applicationCache.GetIdentity(requestId);
                return !identity.IsEmpty
                    ? View("Authn", identity.Value)
                    : View(new IdentityModel());
            }
        }

        [HttpPost]
        [VerifyCaptcha]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Identity(IdentityModel model, SingleSignOnDto sso)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                return await _identityStory.ExecuteAsync(model, HttpContext.GetRequiredHeaders());
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authn(IdentityModel model, SingleSignOnDto sso)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!Configuration.Current.PreAuthnMode)
            {
                return RedirectToAction("Login");
            }

            try
            {
                return await _authnStory.ExecuteAsync(model);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        public async Task<ActionResult> Logout()
        {
            var headers = HttpContext.GetRequiredHeaders();
            await _signOutStory.ExecuteAsync(headers);

            return RedirectToLoginOrIdentity(new SingleSignOnDto());
        }

        [HttpPost]
        public async Task<ActionResult> PostbackFromMfa(string accessToken)
        {
            if (Configuration.Current.PreAuthnMode)
            {
                return await _redirectToCredValidationAfter2FaStory.ExecuteAsync(accessToken);
            }

            return await _authenticateSessionStory.Execute(accessToken);
        }

        private ActionResult RedirectToMfa(string identity, string login, string documentUrl, string samlSessionId, string oidcSessionId,
            ActiveDirectoryCredentialValidationResult validationResult = null)
        {
            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(documentUrl);
            var noLastSegment = $"{currentUri.Scheme}://{currentUri.Authority}";

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing
            var postbackUrl = $"{noLastSegment.Trim("/".ToCharArray())}/{CallbackFromMfa}";

            //extra params
            var claims = new Dictionary<string, string>
            {
                // as specified by user
                { MultiFactorClaims.RawUserName, login }
            };

            if (validationResult != null && validationResult.UserMustChangePassword)
            {
                // if user must change pass, no add sso claims(even if they are present)
                // otherwise callback url will be change and control will not return to ssp
                claims.Add(MultiFactorClaims.ChangePassword, "true");
                // if (Configuration.Current.PreAuthnMode && (oidcSessionId != null || samlSessionId != null))
                // {
                //     if (samlSessionId != null) claims.Add(MultiFactorClaims.SamlSessionId, samlSessionId);
                //     if (oidcSessionId != null) claims.Add(MultiFactorClaims.OidcSessionId, oidcSessionId);
                //     claims.Add(MultiFactorClaims.AdditionSsoStep, "true");
                // }
            }
            else
            {
                if (samlSessionId != null) claims.Add(MultiFactorClaims.SamlSessionId, samlSessionId);
                if (oidcSessionId != null) claims.Add(MultiFactorClaims.OidcSessionId, oidcSessionId);

                // MUST add this claims, otherwise callback url will be change and control will not return to ssp
                if (Configuration.Current.PreAuthnMode && (oidcSessionId != null || samlSessionId != null))
                    claims.Add(MultiFactorClaims.AdditionSsoStep, "true");
            }

            if (validationResult?.PasswordExpirationDate != null)
            {
                claims.Add(MultiFactorClaims.PasswordExpirationDate,
                    validationResult.PasswordExpirationDate.ToString());
            }

            var personalData = new PersonalData(
                validationResult?.DisplayName,
                validationResult?.Email,
                validationResult?.Phone,
                Configuration.Current.PrivacyModeDescriptor);

            var accessPage = _apiClient.CreateAccessRequest(identity,
                personalData.Name,
                personalData.Email,
                personalData.Phone,
                postbackUrl,
                claims);

            return RedirectPermanent(accessPage.Url);
        }

        [HttpGet]
        public async Task<ActionResult> ByPassSsoSession(string callbackUrl, string accessToken)
        {
            var page = new BypassPageDto(callbackUrl, accessToken);
            return View(page);
        }

        public async Task<ActionResult> ByPassSamlSession(string login, string samlSession)
        {
            try
            {
                var request = new BypassSamlRequestDto
                {
                    SamlSessionId = samlSession
                };

                var response = await _multifactorIdpApi.BypassSamlAsync(request, HttpContext.GetRequiredHeaders());

                if (!string.IsNullOrWhiteSpace(response.SamlResponseHtml))
                {
                    return Content(response.SamlResponseHtml, "text/html");
                }

                return RedirectToAction("AccessDenied", "Error");
            }
            catch (UnauthorizedException)
            {
                return RedirectToLoginOrIdentity(new SingleSignOnDto { SamlSessionId = samlSession });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SAML bypass failed for session '{Session}'", samlSession);
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpGet]
        public async Task<ActionResult> ByPassOidcSession(
            string oidcSession)
        {
            try
            {
                var request = new BypassOidcRequestDto
                {
                    OidcSessionId = oidcSession
                };

                var response = await _multifactorIdpApi.BypassOidcAsync(request, HttpContext.GetRequiredHeaders());

                if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
                {
                    return Redirect(response.RedirectUrl);
                }

                return RedirectToAction("AccessDenied", "Error");
            }
            catch (UnauthorizedException)
            {
                return RedirectToLoginOrIdentity(new SingleSignOnDto { OidcSessionId = oidcSession });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "OIDC bypass failed for session '{Session}'", oidcSession);
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        private ActionResult RedirectToLoginOrIdentity(SingleSignOnDto sso)
        {
            return Configuration.Current.PreAuthnMode
                ? RedirectToAction("Identity", sso)
                : RedirectToAction("Login", sso);
        }
    }
}