using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class TotpController : ControllerBase
    {
        [HttpGet]
        public ActionResult Index()
        {
            var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            if (tokenCookie == null)
            {
                return SignOut();
            }

            var api = new MultiFactorSelfServiceApiClient(tokenCookie.Value);

            try
            {
                var totpKey = api.CreateTotpKey();

                return View(new GoogleAuthenticatorModel
                {
                    Link = totpKey.Link,
                    Key = totpKey.Key
                });
            }
            catch (UnauthorizedException)
            {
                return SignOut();
            }
        }

        [HttpPost]
        public ActionResult Add(GoogleAuthenticatorModel model)
        {
            if (ModelState.IsValid)
            {
                var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
                if (tokenCookie == null)
                {
                    return SignOut();
                }

                var api = new MultiFactorSelfServiceApiClient(tokenCookie.Value);

                try
                {
                    var result = api.AddTotpAuthenticator(model.Key, model.Otp);
                    if (result.Success)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    ModelState.AddModelError("Otp", Resources.Totp.WrongOtp);
                }
                catch (UnauthorizedException)
                {
                    return SignOut();
                }
            }

            return View("Index", model);
        }
    }
}