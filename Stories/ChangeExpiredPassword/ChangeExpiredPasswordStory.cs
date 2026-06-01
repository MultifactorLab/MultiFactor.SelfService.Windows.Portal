using System.Threading.Tasks;
using System;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Windows.Portal.ViewModels;
using System.Web.Mvc;
using System.Linq;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;
using System.Security.Claims;

namespace MultiFactor.SelfService.Windows.Portal.Stories.ChangeExpiredPassword
{
    public class ChangeExpiredPasswordStory
    {
        private readonly Configuration _settings;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly DataProtection _dataProtection;
        private readonly UserPasswordChanger _passwordChanger;
        private readonly IApplicationCache _applicationCache;

        public ChangeExpiredPasswordStory(Configuration settings,
            SafeHttpContextAccessor contextAccessor,
            DataProtection dataProtection,
            UserPasswordChanger passwordChanger,
            IApplicationCache applicationCache)
        {
            _settings = settings;
            _contextAccessor = contextAccessor;
            _dataProtection = dataProtection;
            _passwordChanger = passwordChanger;
            _applicationCache = applicationCache;
        }

        public async Task<ActionResult> ExecuteAsync(ChangeExpiredPasswordViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!_settings.EnablePasswordManagement)
            {
                return new RedirectToActionResult().ToActionResult("Login", "Account", new { });
            }

            var principal = _contextAccessor.HttpContext.User as ClaimsPrincipal;
            var rawUserName = principal?.Claims
                .SingleOrDefault(c => c.Type == MultiFactorClaims.RawUserName)?.Value;
            if (rawUserName is null)
            {
                return new RedirectToActionResult().ToActionResult("Login", "Account", new { });
            }

            var userName = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
            var encryptedPwd = _applicationCache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));

            if (userName?.Value is null || encryptedPwd?.Value is null)
            {
                return new RedirectToActionResult().ToActionResult("Login", "Account", new { });
            }

            var currentPassword = _dataProtection.Unprotect(encryptedPwd.Value, Constants.PWD_RENEWAL_PURPOSE);
            var pwdChangeResult = await _passwordChanger.ChangePassword(
                userName.Value,
                currentPassword,
                model.NewPassword);

            if (!pwdChangeResult.Success)
            {
                throw new ModelStateErrorException(pwdChangeResult.ErrorReason);
            }

            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(rawUserName));
            _applicationCache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(rawUserName));

            return new RedirectToActionResult().ToActionResult("Done", "ExpiredPassword", new { });
        }
    }
}