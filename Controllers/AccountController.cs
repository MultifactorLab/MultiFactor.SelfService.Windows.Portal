using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using Resources;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class AccountController : ControllerBase
    {
        private readonly ApplicationCache _applicationCache;
        private readonly AuthService _authService;
        private readonly MultiFactorApiClient _apiClient;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly DataProtectionService _dataProtectionService;
        private readonly ILogger _logger;
        private const string CallbackFromMfa = "PostbackFromMfa";

        public AccountController(ApplicationCache applicationCache,
            AuthService authService,
            MultiFactorApiClient apiClient,
            ActiveDirectoryService activeDirectoryService,
            DataProtectionService dataProtectionService,
            ILogger logger, IHttpClientFactory httpFactory)
        {
            _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _activeDirectoryService =
                activeDirectoryService ?? throw new ArgumentNullException(nameof(activeDirectoryService));
            _dataProtectionService =
                dataProtectionService ?? throw new ArgumentNullException(nameof(dataProtectionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpFactory = httpFactory;
        }

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

            if (Configuration.Current.RequiresUpn)
            {
                //AD requires UPN check
                var userName = LdapIdentity.ParseUser(model.UserName);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    ModelState.AddModelError(string.Empty, AccountLogin.UserNameUpnRequired);
                    return View(model);
                }
            }

            //AD credential check
            var adValidationResult =
                _activeDirectoryService.VerifyCredentialAndMembership(model.UserName.Trim(), model.Password.Trim());
            // credential is VALID
            if (adValidationResult.IsAuthenticated)
            {
                var identity = adValidationResult.GetIdentity(model.UserName);
                if (sso.HasSamlSession() && adValidationResult.IsBypass)
                {
                    return ByPassSamlSession(identity, sso.SamlSessionId);
                }

                return RedirectToMfa(
                    identity: identity,
                    login: model.UserName,
                    documentUrl: model.MyUrl,
                    samlSessionId: sso.SamlSessionId,
                    oidcSessionId: sso.OidcSessionId,
                    validationResult: adValidationResult
                );
            }

            if (adValidationResult.UserMustChangePassword)
            {
                // because if we here - bind throw exception, so need verify
                adValidationResult = _activeDirectoryService.VerifyMembership(LdapIdentity.ParseUser(model.UserName));
                var identity = adValidationResult.GetIdentity(model.UserName);
                _logger.Warning("User's credentials are valid but user '{u:l}' must change password", identity);

                if (Configuration.Current.EnablePasswordManagement)
                {
                    var encryptedPassword = _dataProtectionService.Protect(model.Password.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName.Trim()),
                        model.UserName.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName.Trim()),
                        encryptedPassword);

                    return RedirectToMfa(identity, model.UserName, model.MyUrl, null, null, adValidationResult);
                }

                _logger.Warning("User '{u:l}' must change password but password management is not enabled", identity);
            }

            ModelState.AddModelError(string.Empty, AccountLogin.WrongUserNameOrPassword);

            // invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));

            return View(model);
        }

        /// <summary>
        /// 2-step user verification: 2fa then AD credentials (first factor)
        /// </summary>
        /// <param name="sso">Model for sso integration. Can be empty.</param>
        /// <param name="requestId">State for continuation user verification.</param>
        /// <returns></returns>
        public ActionResult Identity(SingleSignOnDto sso, string requestId)
        {
            if (!Configuration.Current.PreAuthnMode)
            {
                return RedirectToAction("Login");
            }

            bool userAuthenticated = Request.IsAuthenticated;
            //integrated windows authentication
            bool authenticateWindowsUser =
                Configuration.AuthenticationMode == AuthenticationMode.Windows && User.Identity != null;
            bool negotiateAuthentication = !string.IsNullOrEmpty(User.Identity?.Name) &&
                                           User.Identity.AuthenticationType == "Negotiate";

            if (!userAuthenticated || !authenticateWindowsUser || !negotiateAuthentication)
            {

                var identity = _applicationCache.GetIdentity(requestId);
                return !identity.IsEmpty
                    ? View("Authn", identity.Value)
                    : View(new IdentityModel());
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
        public ActionResult Identity(IdentityModel model, SingleSignOnDto sso)
        {
            if (!Configuration.Current.PreAuthnMode)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (Configuration.Current.RequiresUpn)
            {
                //AD requires UPN check
                var userName = LdapIdentity.ParseUser(model.UserName);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    ModelState.AddModelError(string.Empty, AccountLogin.UserNameUpnRequired);
                    return View(model);
                }
            }

            // 2fa before authn
            var identity = model.UserName;
            // in common case
            if (!Configuration.Current.NeedPrebindInfo())
            {
                return RedirectToMfa(
                    identity: identity,
                    login: model.UserName,
                    documentUrl: model.MyUrl,
                    samlSessionId: sso.SamlSessionId,
                    oidcSessionId: sso.OidcSessionId
                );
            }

            var adResult = _activeDirectoryService.VerifyMembership(LdapIdentity.ParseUser(model.UserName.Trim()));

            identity = adResult.GetIdentity(model.UserName.Trim());

            // sso session can skip 2fa, so go to pass entered
            if (adResult.IsBypass && sso.HasSamlSession())
            {
                return View("Authn", model);
            }

            return RedirectToMfa(
                identity: identity,
                login: model.UserName,
                documentUrl: model.MyUrl,
                samlSessionId: sso.SamlSessionId,
                oidcSessionId: sso.OidcSessionId,
                validationResult: adResult
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Authn(IdentityModel model, SingleSignOnDto sso)
        {
            if (!Configuration.Current.PreAuthnMode)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View("Authn", model);
            }

            if (Configuration.Current.RequiresUpn)
            {
                //AD requires UPN check
                var userName = LdapIdentity.ParseUser(model.UserName);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    ModelState.AddModelError(string.Empty, AccountLogin.UserNameUpnRequired);
                    return View(model);
                }
            }

            // authn after 2fa
            // AD credential check
            var adValidationResult = _activeDirectoryService.VerifyCredentialAndMembership(model.UserName.Trim(), model.Password.Trim());
            // credential is VALID
            if (adValidationResult.IsAuthenticated)
            {
                var identity = adValidationResult.GetIdentity(model.UserName);
                if (sso.HasSamlSession())
                {
                    if (adValidationResult.IsBypass)
                    {
                        return ByPassSamlSession(identity, sso.SamlSessionId);
                    }

                    // go to idp, return and render html form with saml assertion
                    return await GetSamlAssertion(model.AccessToken);
                }

                _authService.SignIn(model.AccessToken);
                return RedirectToAction("Index", "Home");
            }

            if (adValidationResult.UserMustChangePassword)
            {
                // if we need upn or custom attribute, we MUST request it from AD one more time
                // because for expired password bind is failed
                if (Configuration.Current.UseUpnAsIdentity || !string.IsNullOrWhiteSpace(Configuration.Current.UseAttributeAsIdentity))
                {
                    adValidationResult = _activeDirectoryService.VerifyMembership(LdapIdentity.ParseUser(model.UserName));
                }

                var identity = adValidationResult.GetIdentity(model.UserName);
                _logger.Warning("User's credentials are valid but user '{u:l}' must change password", identity);

                if (Configuration.Current.EnablePasswordManagement)
                {
                    var encryptedPassword = _dataProtectionService.Protect(model.Password.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName.Trim()),
                        model.UserName.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName.Trim()),
                        encryptedPassword);
                    // for change password redirect
                    _authService.SignIn(model.AccessToken);

                    return RedirectToAction("Index", "Home");
                }

                _logger.Warning("User '{u:l}' must change password but password management is not enabled",
                    identity);
            }

            ModelState.AddModelError(string.Empty, AccountLogin.WrongUserNameOrPassword);

            // invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));

            return View("Authn", model);
        }

        public ActionResult Logout()
        {
            if (Configuration.AuthenticationMode == AuthenticationMode.Forms)
            {
                return SignOut();
            }

            SignOut();
            return View();
        }

        [HttpPost]
        public ActionResult PostbackFromMfa(string accessToken)
        {
            _logger.Debug($"Received MFA token: {accessToken}");

            // 2fa before authn enable
            if (Configuration.Current.PreAuthnMode)
            {
                // hence continue authentication flow
                return RedirectToCredValidationAfter2FA(accessToken);
            }

            // otherwise flow is (almost) finished
            _authService.SignIn(accessToken);
            return RedirectToAction("Index", "Home");
        }

        /*
         * Now we know: username, the fact of successful confirmation of the 2fa and some info about user.
         * Next step - enter password and verify user creds.
         * For this we must correctly pass all known information using the cache and query params.
         */
        private ActionResult RedirectToCredValidationAfter2FA(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);
            var usernameClaims = token.Claims.FirstOrDefault(claim => claim.Type == MultiFactorClaims.RawUserName);

            // for the password entry step
            var requestId = token.Id;
            _applicationCache.SetIdentity(requestId,
                new IdentityModel { UserName = usernameClaims?.Value, AccessToken = accessToken });

            object routeValue = new { requestId = requestId };

            #region Process SSO session (if present)

            var oidcClaims = token.Claims.FirstOrDefault(claim => claim.Type == MultiFactorClaims.OidcSessionId);
            var samlClaims = token.Claims.FirstOrDefault(claim => claim.Type == MultiFactorClaims.SamlSessionId);
            if (!string.IsNullOrEmpty(samlClaims?.Value))
            {
                routeValue = new { samlSessionId = samlClaims?.Value, requestId = requestId };
                return RedirectToAction("Identity", "Account", routeValue);
            }

            if (!string.IsNullOrEmpty(oidcClaims?.Value))
            {
                routeValue = new { oidcSessionId = oidcClaims?.Value, requestId = requestId };
                return RedirectToAction("Identity", "Account", routeValue);
            }

            #endregion

            return RedirectToAction("Identity", "Account", routeValue);
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

        private ActionResult ByPassSamlSession(string login, string samlSessionId)
        {
            var bypassPage = _apiClient.CreateSamlBypassRequest(login, samlSessionId);
            return View("ByPassSamlSession", bypassPage);
        }

        private async Task<ActionResult> GetSamlAssertion(string accessToken)
        {
            // no token verification because 'aud'=api_key and ssp_api_key!=saml_api_key
            // hence verification will fail. but it's ok, idp service make its own verification
            // so security not broken
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);
            var idpUrl = token.Claims.FirstOrDefault(claim => claim.Type == MultiFactorClaims.AdditionSsoStep)
                ?.Value;

            try
            {
                MultipartFormDataContent multipartContent = new MultipartFormDataContent
                {
                    {
                        new StringContent(accessToken, Encoding.UTF8, MediaTypeNames.Text.Plain),
                        "accessToken"
                    }
                };
                HttpClient httpClient = _httpFactory.CreateClient(Constants.HttpClients.MultifactorIdpApi);
                var res = await httpClient.PostAsync(idpUrl, multipartContent);
                var jsonResponse = await res.Content.ReadAsStringAsync();
                return Content(jsonResponse);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {idpUrl}: {ex.Message}");
                throw;
            }
        }
    }
}