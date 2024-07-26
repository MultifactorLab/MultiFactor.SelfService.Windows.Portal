using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly DataProtectionService _dataProtectionService;
        private readonly ILogger _logger;
        private const string CallbackFromMfa = "PostbackFromMfa";

        public AccountController(ApplicationCache applicationCache,
            AuthService authService,
            MultiFactorApiClient apiClient,
            ActiveDirectoryService activeDirectoryService,
            DataProtectionService dataProtectionService,
            ILogger logger)
        {
            _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _activeDirectoryService = activeDirectoryService ?? throw new ArgumentNullException(nameof(activeDirectoryService));
            _dataProtectionService = dataProtectionService ?? throw new ArgumentNullException(nameof(dataProtectionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            return RedirectToMfa(userName, Request?.Url?.ToString(), sso.SamlSessionId, sso.OidcSessionId);
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
            var adValidationResult = _activeDirectoryService.VerifyCredentialAndMembership(model.UserName.Trim(), model.Password.Trim());
            // credential is VALID
            if (adValidationResult.IsAuthenticated)
            {
                var identity = GetIdentity(model.UserName, adValidationResult);
                if (sso.HasSamlSession() && adValidationResult.IsBypass)
                {
                    return ByPassSamlSession(identity, sso.SamlSessionId);
                }
                return RedirectToMfa(identity, model.MyUrl, sso.SamlSessionId, sso.OidcSessionId, adValidationResult);
            }

            if (adValidationResult.UserMustChangePassword)
            {
                var identity = GetIdentity(model.UserName, adValidationResult);
                _logger.Warning("User's credentials are valid but user '{u:l}' must change password", identity);

                if (Configuration.Current.EnablePasswordManagement)
                {
                    var encryptedPassword = _dataProtectionService.Protect(model.Password.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(identity), model.UserName.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(identity), encryptedPassword);

                    return RedirectToMfa(identity, model.MyUrl, null, null, adValidationResult);
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
            return RedirectToMfa(userName, Request?.Url?.ToString(), sso.SamlSessionId, sso.OidcSessionId);
        }

        [HttpPost]
        [VerifyCaptcha]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Identity(IdentityModel model, SingleSignOnDto sso)
        {
            if (!Configuration.Current.PreAuthnMode)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return string.IsNullOrEmpty(model.AccessToken) ? View(model) : View("Authn", model);
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
            bool secondFactorNotProceededYet = string.IsNullOrWhiteSpace(model.AccessToken);
            if (secondFactorNotProceededYet)
            {
                var identity = model.UserName;
                if (!Configuration.Current.UseUpnAsIdentity && !Configuration.Current.ActiveDirectory2FaGroup.Any())
                {
                    return RedirectToMfa(identity, model.MyUrl, sso.SamlSessionId, sso.OidcSessionId);
                }
                var adResult = _activeDirectoryService.VerifyMembership(LdapIdentity.ParseUser(model.UserName.Trim()));

                if (Configuration.Current.UseUpnAsIdentity)
                {
                    identity = adResult.Upn;
                }

                if (adResult.IsBypass && sso.HasSamlSession())
                {
                    return ByPassSamlSession(identity, sso.SamlSessionId);
                }

                return RedirectToMfa(identity, model.MyUrl, sso.SamlSessionId, sso.OidcSessionId, adResult);
            }

            // authn after 2fa
            //AD credential check
            var adValidationResult =
                _activeDirectoryService.VerifyCredentialAndMembership(model.UserName.Trim(), model.Password.Trim());
            // credential is VALID
            if (adValidationResult.IsAuthenticated)
            {
                var identity = GetIdentity(model.UserName, adValidationResult);
                if (sso.HasSamlSession() && adValidationResult.IsBypass)
                {
                    return ByPassSamlSession(identity, sso.SamlSessionId);
                }

                _authService.SignIn(model.AccessToken);
                return RedirectToAction("Index", "Home");
            }

            if (adValidationResult.UserMustChangePassword)
            {
                // if we need upn, we MUST request it from AD one more time
                // because for expired password bind is failed
                if (Configuration.Current.UseUpnAsIdentity)
                {
                    adValidationResult =
                        _activeDirectoryService.VerifyMembership(LdapIdentity.ParseUser(model.UserName));
                }
                var identity = GetIdentity(model.UserName, adValidationResult);
                _logger.Warning("User's credentials are valid but user '{u:l}' must change password", identity);

                if (Configuration.Current.EnablePasswordManagement)
                {
                    var encryptedPassword = _dataProtectionService.Protect(model.Password.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(identity),
                        model.UserName.Trim());
                    _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(identity),
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

            return View(model);
        }

        private static string GetIdentity(string userName,
            ActiveDirectoryCredentialValidationResult adValidationResult)
        {
            var identity = userName;
            if (Configuration.Current.UseUpnAsIdentity)
            {
                if (string.IsNullOrEmpty(adValidationResult.Upn))
                {
                    throw new InvalidOperationException($"Null UPN for user {userName}");
                }

                identity = adValidationResult.Upn;
            }

            return identity;
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

        private ActionResult RedirectToMfa(string login, string documentUrl, string samlSessionId, string oidcSessionId,
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
                claims.Add(MultiFactorClaims.ChangePassword, "true");
            }
            else
            {
                if (samlSessionId != null) claims.Add(MultiFactorClaims.SamlSessionId, samlSessionId);
                if (oidcSessionId != null) claims.Add(MultiFactorClaims.OidcSessionId, oidcSessionId);
            }

            if (validationResult?.PasswordExpirationDate != null)
            {
                claims.Add(MultiFactorClaims.PasswordExpirationDate, validationResult.PasswordExpirationDate.ToString());
            }

            var accessPage = _apiClient.CreateAccessRequest(login,
                validationResult?.DisplayName,
                validationResult?.Email,
                validationResult?.Phone,
                postbackUrl, claims);

            return RedirectPermanent(accessPage.Url);
        }

        private ActionResult ByPassSamlSession(string login, string samlSessionId)
        {
            var bypassPage = _apiClient.CreateSamlBypassRequest(login, samlSessionId);
            return View("ByPassSamlSession", bypassPage);
        }

        [HttpPost]
        public ActionResult PostbackFromMfa(string accessToken)
        {
            _logger.Debug($"Received MFA token: {accessToken}");
            // 2fa before authn enable
            
            if (Configuration.Current.PreAuthnMode)
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(accessToken);
                var claims = token.Claims.FirstOrDefault(claim => claim.Type == MultiFactorClaims.RawUserName);
                var requestId = token.Id;
                _applicationCache.SetIdentity(requestId,
                    new IdentityModel
                        { UserName = claims?.Value, AccessToken = accessToken });
                return RedirectToAction("Identity", "Account", new { requestId = requestId });
            }

            _authService.SignIn(accessToken);
            return RedirectToAction("Index", "Home");
        }
    }
}