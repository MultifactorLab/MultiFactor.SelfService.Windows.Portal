using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class PasswordController : Controller
    {
        [HttpGet]
        public ActionResult Change()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Change(ChangePasswordModel model)
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
}