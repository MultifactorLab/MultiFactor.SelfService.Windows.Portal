using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;
using Serilog;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized(false)]
    [RequiredFeature(ApplicationFeature.PasswordManagement)]
    public class ExpiredPasswordController : ControllerBase
    {
        private readonly ApplicationCache _applicationCache;
        private readonly MultiFactorApiClient _api;
        private readonly AuthService _authService;
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly DataProtectionService _dataProtectionService;
        private readonly JwtTokenProvider _tokenProvider;
        private readonly ILogger _logger;
        private readonly PasswordPolicyService _passwordPolicyService;

        public ExpiredPasswordController(ApplicationCache applicationCache, 
            MultiFactorApiClient api, 
            AuthService authService,
            ActiveDirectoryService activeDirectoryService,
            DataProtectionService dataProtectionService,
            JwtTokenProvider tokenProvider, 
            ILogger logger,
            PasswordPolicyService passwordPolicyService)
        {
            _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _activeDirectoryService = activeDirectoryService ?? throw new ArgumentNullException(nameof(activeDirectoryService));
            _dataProtectionService = dataProtectionService ?? throw new ArgumentNullException(nameof(dataProtectionService));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _passwordPolicyService = passwordPolicyService ?? throw new ArgumentNullException(nameof(passwordPolicyService));
            
        }

        [HttpGet]
        public ActionResult Change()
        {
            var userName = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(User.Identity.Name));
            var encryptedPwd = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(User.Identity.Name));

            if (userName.IsEmpty || encryptedPwd.IsEmpty)
            {
                throw new PasswordChangingSessionExpired(User.Identity.Name);
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Change(ChangeExpiredPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, Resources.PasswordChange.WrongUserNameOrPassword);
                return View(model);
            }
            
            var userName = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(User.Identity.Name));
            var encryptedPwd = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(User.Identity.Name));
            if (userName.IsEmpty || encryptedPwd.IsEmpty)
            {
                throw new PasswordChangingSessionExpired(User.Identity.Name);
            }
            
            if (!_passwordPolicyService.IsPasswordValid(model.NewPassword, out string errorReason))
            {
                _logger.Error("Unable to change expired password for user '{u:l}'. Failed to set new password: {err:l}", userName.Value, errorReason);
                ModelState.AddModelError(nameof(model.NewPassword), errorReason);
                return View(model);
            }

            var currentPassword = _dataProtectionService.Unprotect(encryptedPwd.Value);
            if (!_activeDirectoryService.ChangeExpiredPassword(userName.Value, currentPassword, model.NewPassword, out errorReason))
            {
                ModelState.AddModelError(string.Empty, errorReason);
                return View(model);
            }

            var newToken = _api.RefreshAccessToken(_tokenProvider.GetToken(), new Dictionary<string, string>
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

            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(User.Identity.Name));
            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(User.Identity.Name));

            return RedirectToAction("Done");
        }

        public ActionResult Done()
        {
            AppAuthentication.SignOut();
            return View();
        }
    }
}