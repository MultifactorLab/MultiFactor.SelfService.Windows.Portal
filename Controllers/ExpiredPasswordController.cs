using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Authentication;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized(false)]
    [RequiredFeature(ApplicationFeature.PasswordManagement)]
    public class ExpiredPasswordController : ControllerBase
    {
        private readonly ApplicationCache _applicationCache;
        private readonly MultiFactorApiClient _api;
        private readonly AuthenticateSessionStory _authenticateSessionStory;
        private readonly AuthService _authService;
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly DataProtectionService _dataProtectionService;
        private readonly JwtTokenProvider _tokenProvider;
        private readonly TokenVerifier _tokenVerifier;
        private readonly ILogger _logger;
        private readonly PasswordPolicyService _passwordPolicyService;

        public ExpiredPasswordController(ApplicationCache applicationCache, 
            MultiFactorApiClient api, 
            AuthenticateSessionStory authenticateSessionStory,
            AuthService authService,
            ActiveDirectoryService activeDirectoryService,
            DataProtectionService dataProtectionService,
            JwtTokenProvider tokenProvider, 
            TokenVerifier tokenVerifier,
            ILogger logger,
            PasswordPolicyService passwordPolicyService)
        {
            _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _authenticateSessionStory = authenticateSessionStory ?? throw new ArgumentNullException(nameof(authenticateSessionStory));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _activeDirectoryService = activeDirectoryService ?? throw new ArgumentNullException(nameof(activeDirectoryService));
            _dataProtectionService = dataProtectionService ?? throw new ArgumentNullException(nameof(dataProtectionService));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _tokenVerifier = tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordPolicyService = passwordPolicyService ?? throw new ArgumentNullException(nameof(passwordPolicyService));
            
        }

        [HttpGet]
        public ActionResult Change()
        {
            var rawUserName = _tokenVerifier.Verify(_tokenProvider.GetToken()).RawUserName ?? User.Identity.Name;
            var userName = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
            var encryptedPwd = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));

            if (userName.IsEmpty || encryptedPwd.IsEmpty)
            {
                throw new PasswordChangingSessionExpired(User.Identity.Name);
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Change(ChangeExpiredPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, Resources.PasswordChange.WrongUserNameOrPassword);
                return View(model);
            }
            
            var accessToken = _tokenProvider.GetToken();
            var tokenClaims = _tokenVerifier.Verify(accessToken);
            var rawUserName = tokenClaims.RawUserName ?? User.Identity.Name;
            var userName = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
            var encryptedPwd = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));
            if (userName.IsEmpty || encryptedPwd.IsEmpty)
            {
                throw new PasswordChangingSessionExpired(User.Identity.Name);
            }
            
            var validationResult = _passwordPolicyService.ValidatePassword(model.NewPassword);
            if (!validationResult.IsValid)
            {
                _logger.Warning("Unable to change expired password for user '{u:l}'. Failed to set new password: {err:l}", userName.Value, validationResult);
                ModelState.AddModelError(nameof(model.NewPassword), validationResult.ToString());
                return View(model);
            }

            var currentPassword = _dataProtectionService.Unprotect(encryptedPwd.Value);
            if (!_activeDirectoryService.ChangeExpiredPassword(userName.Value, currentPassword, model.NewPassword, out string errorReason))
            {
                ModelState.AddModelError(string.Empty, errorReason);
                return View(model);
            }

            if (string.IsNullOrEmpty(tokenClaims.SamlClaim) && string.IsNullOrEmpty(tokenClaims.OidcClaim))
            {
                var newToken = _api.RefreshAccessToken(accessToken, new Dictionary<string, string>
                {
                    { MultiFactorClaims.RawUserName, User.Identity.Name }
                });
                try
                {
                    AppAuthentication.SignOut();
                    _authService.SignIn(newToken);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Unable to sign-in with the refreshed token for user '{u:l}': {m:l}", userName.Value, ex.Message);
                    ModelState.AddModelError(string.Empty, Resources.PasswordChange.WrongUserNameOrPassword);
                    return View(model);
                }

                _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
                _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));
                return RedirectToAction("Done");
            }

            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));

            try
            {
                var claims = _tokenVerifier.GetPasswordChangeClaims(accessToken);
                claims.Remove(MultiFactorClaims.PasswordExpirationDate);
                var newToken = _api.RefreshAccessToken(accessToken, claims);
                var refreshedClaims = _tokenVerifier.Verify(newToken);
                if (refreshedClaims.MustChangePassword
                    || refreshedClaims.Identity != tokenClaims.Identity
                    || refreshedClaims.Id != tokenClaims.Id
                    || refreshedClaims.RawUserName != rawUserName
                    || refreshedClaims.SamlClaim != tokenClaims.SamlClaim
                    || refreshedClaims.OidcClaim != tokenClaims.OidcClaim)
                {
                    throw new InvalidOperationException("Refreshed token has unexpected claims.");
                }

                AppAuthentication.SignOut();
                return await _authenticateSessionStory.ExecuteAfterPasswordChangeAsync(
                    newToken, tokenClaims.SamlClaim, tokenClaims.OidcClaim);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Unable to complete sign-in after password change for user '{u:l}'", userName.Value);
                AppAuthentication.SignOut();
                return RedirectToAction("Login", "Account",
                    new { samlSessionId = tokenClaims.SamlClaim, oidcSessionId = tokenClaims.OidcClaim });
            }
        }

        public ActionResult Done()
        {
            AppAuthentication.SignOut();
            return View();
        }
    }
}
