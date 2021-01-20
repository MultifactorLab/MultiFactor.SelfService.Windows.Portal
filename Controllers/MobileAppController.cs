using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class MobileAppController : ControllerBase
    {
        public ActionResult Index()
        {
            var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            if (tokenCookie != null)
            {
                var tokenValidationService = new TokenValidationService();
                if (tokenValidationService.VerifyToken(tokenCookie.Value, out var token))
                {
                    return View(token);
                }
            }

            return SignOut();
        }
    }
}