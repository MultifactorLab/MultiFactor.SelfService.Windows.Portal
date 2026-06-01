using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Core;
using System.Linq;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;
using System.Security.Claims;

namespace MultiFactor.SelfService.Windows.Portal.Stories.CheckExpiredPasswordSession
{
    public class CheckExpiredPasswordSessionStory
    {
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly Configuration _settings;

        private readonly IApplicationCache _applicationCache;

        public CheckExpiredPasswordSessionStory(SafeHttpContextAccessor contextAccessor, Configuration settings, IApplicationCache applicationCache)
        {
            _contextAccessor = contextAccessor;
            _settings = settings;
            _applicationCache = applicationCache;
        }

        public ActionResult Execute()
        {
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

            if (userName.IsEmpty || encryptedPwd.IsEmpty)
            {
                return new RedirectToActionResult().ToActionResult("Login", "Account", new { });
            }

            return new ViewResult();
        }
    }
}
