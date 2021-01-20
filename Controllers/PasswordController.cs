using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class PasswordController : ControllerBase
    {
        [HttpGet]
        public ActionResult Change()
        {
            var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            if (tokenCookie != null)
            {
                var tokenValidationService = new TokenValidationService();
                if (tokenValidationService.VerifyToken(tokenCookie.Value, out var token))
                {
                    return View();
                }
            }

            return SignOut();
        }

        [HttpPost]
        public ActionResult Change(ChangePasswordModel model)
        {
            var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
            if (tokenCookie != null)
            {
                var tokenValidationService = new TokenValidationService();
                if (tokenValidationService.VerifyToken(tokenCookie.Value, out var token))
                {
                    if (ModelState.IsValid)
                    {
                        var activeDirectoryService = new ActiveDirectoryService();
                        if (activeDirectoryService.ChangePassword(User.Identity.Name, model.Password, model.NewPassword, out string errorReason))
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, errorReason);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
                    }
                    return View(model);
                }
            }

            return SignOut();
        }
    }
}