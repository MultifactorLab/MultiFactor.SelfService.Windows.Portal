using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using Serilog;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;

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
            if (Request.IsAuthenticated)
            {
                if (Configuration.AuthenticationMode == AuthenticationMode.Windows && User.Identity != null)
                {
                    //integrated windows authentication
                    if (!string.IsNullOrEmpty(User.Identity.Name) && User.Identity.AuthenticationType == "Negotiate")
                    {
                        var userName = User.Identity.Name;

                        _logger.Information("User '{user:l}' authenticated by NTLM/Kerberos", userName);
                        return RedirectToMfa(userName, Request.Url.ToString(), sso.SamlSessionId, sso.OidcSessionId);
                    }
                }
            }

            return View(new LoginModel());
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
                    ModelState.AddModelError(string.Empty, Resources.AccountLogin.UserNameUpnRequired);
                    return View(model);
                }
            }

            //AD credential check
            var adValidationResult = _activeDirectoryService.VerifyCredential(model.UserName.Trim(), model.Password.Trim());
            // credential is VALID
            if (adValidationResult.IsAuthenticated)
            {
                var identity = GetIdentity(model, adValidationResult);
                if (sso.HasSamlSession() && adValidationResult.IsBypass)
                {
                    return ByPassSamlSession(identity, sso.SamlSessionId);
                }
                return RedirectToMfa(identity, model.MyUrl, sso.SamlSessionId, sso.OidcSessionId, adValidationResult);
            }

            if (adValidationResult.UserMustChangePassword)
            {
                var identity = GetIdentity(model, adValidationResult);
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

            ModelState.AddModelError(string.Empty, Resources.AccountLogin.WrongUserNameOrPassword);

            // invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));

            return View(model);
        }

        private static string GetIdentity(LoginModel model, ActiveDirectoryCredentialValidationResult adValidationResult)
        {
            var identity = model.UserName;
            if (Configuration.Current.UseUpnAsIdentity)
            {
                if (string.IsNullOrEmpty(adValidationResult.Upn))
                {
                    throw new InvalidOperationException($"Null UPN for user {model.UserName}");
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
            var noLastSegment = string.Format("{0}://{1}", currentUri.Scheme, currentUri.Authority);

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing
            var postbackUrl = $"{noLastSegment.Trim("/".ToCharArray())}/PostbackFromMfa";

            //exra params
            var claims = new Dictionary<string, string>
            {
                // as specifyed by user
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

            if (validationResult.PasswordExpirationDate != null)
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
            _authService.SignIn(accessToken);
            return RedirectToAction("Index", "Home");
        }
    }
}