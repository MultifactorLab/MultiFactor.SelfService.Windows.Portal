using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class ExpiredPasswordController : ControllerBase
    {
        [HttpGet]
        public ActionResult Change()
        {
            var userName = Session[Constants.SESSION_EXPIRED_PASSWORD_USER_KEY] as string;
            var encryptedPwd = Session[Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY] as string;

            if (userName == null || encryptedPwd == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!Configuration.Current.EnablePasswordManagement)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Change(ChangeExpiredPasswordModel model)
        {
            if (!Configuration.Current.EnablePasswordManagement)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                var userName = Session[Constants.SESSION_EXPIRED_PASSWORD_USER_KEY] as string;
                var encryptedPwd = Session[Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY] as string;
                if (userName == null || encryptedPwd == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var dataProtectionService = new DataProtectionService();
                var currentPassword = dataProtectionService.Unprotect(encryptedPwd);

                var activeDirectoryService = new ActiveDirectoryService();
                if (activeDirectoryService.ChangePassword(userName, currentPassword, model.NewPassword, false, out string errorReason))
                {
                    Session.Remove(Constants.SESSION_EXPIRED_PASSWORD_USER_KEY);
                    Session.Remove(Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY);

                    return RedirectToAction("Done");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, errorReason);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, Resources.PasswordChange.WrongUserNameOrPassword);
            }

            return View(model);
        }

        public ActionResult Done()
        {
            return View();
        }
    }
}