using MultiFactor.SelfService.Windows.Portal.Services.API;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class HomeController : ControllerBase
    {
        public ActionResult Index()
        {
            if (Request.QueryString["samlSessionId"] != null)
            {
                //re-login for saml authentication
                return SignOut();
            }

            var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            if (tokenCookie == null)
            {
                return SignOut();
            }

            try
            {
                var api = new MultiFactorSelfServiceApiClient(tokenCookie.Value);
                var userProfile = api.LoadProfile();
                userProfile.EnablePasswordManagement = Configuration.Current.EnablePasswordManagement;
                userProfile.EnableExchangeActiveSyncDevicesManagement = Configuration.Current.EnableExchangeActiveSyncDevicesManagement;

                return View(userProfile);
            }
            catch (UnauthorizedException)
            {
                return SignOut();
            }
        }

        [HttpPost]
        public ActionResult RemoveAuthenticator(string authenticator, string id)
        {
            var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            if (tokenCookie == null)
            {
                return SignOut();
            }

            var api = new MultiFactorSelfServiceApiClient(tokenCookie.Value);

            try
            {
                var userProfile = api.LoadProfile();
                if (userProfile.Count > 1) //do not remove last
                {
                    api.RemoveAuthenticator(authenticator, id);
                }

                return RedirectToAction("Index");
            }
            catch (UnauthorizedException)
            {
                return SignOut();
            }
        }
    }
}