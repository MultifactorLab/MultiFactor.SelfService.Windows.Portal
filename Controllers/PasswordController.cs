using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    [RequiredFeature(ApplicationFeature.PasswordManagement)]
    public class PasswordController : ControllerBase
    {
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly PasswordPolicyService _passwordPolicyService;

        public PasswordController(
            ActiveDirectoryService activeDirectoryService,
            PasswordPolicyService passwordPolicyService)
        {
            _activeDirectoryService = activeDirectoryService ?? throw new System.ArgumentNullException(nameof(activeDirectoryService));
            _passwordPolicyService = passwordPolicyService ?? throw new System.ArgumentNullException(nameof(passwordPolicyService));
        }

        [HttpGet]
        public ActionResult Change() => View();
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Change(ChangePasswordModel model)
        {      
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, Resources.PasswordChange.WrongUserNameOrPassword);
                return View(model);
            }

            var portalValidationResult = _passwordPolicyService.ValidatePassword(model.NewPassword, User.Identity.Name);
            if (!portalValidationResult.IsValid)
            {
                ModelState.AddModelError(nameof(model.NewPassword), portalValidationResult.ErrorMessage);
                return View(model);
            }
            
            
            if (!_activeDirectoryService.ChangeValidPassword(User.Identity.Name, model.Password, model.NewPassword, out string errorReason))
            {
                ModelState.AddModelError(string.Empty, errorReason);
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}