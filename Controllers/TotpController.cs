using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    public class TotpController : ControllerBase
    {
        private readonly MultiFactorSelfServiceApiClient _api;

        public TotpController(MultiFactorSelfServiceApiClient api)
        {
            _api = api ?? throw new System.ArgumentNullException(nameof(api));
        }

        [HttpGet]
        public ActionResult Index()
        {            
            var totpKey = _api.CreateTotpKey();
            return View(new GoogleAuthenticatorModel
            {
                Link = totpKey.Link,
                Key = totpKey.Key
            });
        }

        [HttpPost]
        public ActionResult Add(GoogleAuthenticatorModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }
     
            var result = _api.AddTotpAuthenticator(model.Key, model.Otp);
            if (!result.Success)
            {
                ModelState.AddModelError("Otp", Resources.Totp.WrongOtp);
                return View("Index", model);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}