using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using System;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    public class HomeController : ControllerBase
    {
        private readonly MultiFactorSelfServiceApiClient _api;

        public HomeController(MultiFactorSelfServiceApiClient api)
        {
            _api = api ?? throw new System.ArgumentNullException(nameof(api));
        }

        public ActionResult Index()
        {
            if (Request.QueryString[MultiFactorClaims.SamlSessionId] != null)
            {
                //re-login for saml authentication
                return SignOut();
            }
            if (Request.QueryString[MultiFactorClaims.OidcSessionId] != null)
            {
                //re-login for oidc authentication
                return SignOut();
            }

            var userProfile = _api.LoadUserProfile();
            userProfile.EnablePasswordManagement = Configuration.Current.EnablePasswordManagement;
            userProfile.EnableExchangeActiveSyncDevicesManagement = Configuration.Current.EnableExchangeActiveSyncDevicesManagement;
            var expiration = (DateTime?)HttpContext.Items["passwordExpirationDate"];
            if (expiration != null)
            {
                userProfile.PasswordExpirationDaysLeft = (expiration - DateTime.Now).Value.Days;
            }
            return View(userProfile);
        }

        [HttpPost]
        public ActionResult RemoveAuthenticator(string authenticator, string id)
        {
            var userProfile = _api.LoadUserProfile();
            if (userProfile.Count > 1) //do not remove last
            {
                _api.RemoveAuthenticator(authenticator, id);
            }

            return RedirectToAction("Index");
        }
    }
}