﻿using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    [RequiredFeature(ApplicationFeature.PasswordManagement)]
    public class PasswordController : ControllerBase
    {
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly PasswordPolicyService _passwordPolicyService;
        private readonly ILogger _logger;

        public PasswordController(
            ActiveDirectoryService activeDirectoryService,
            PasswordPolicyService passwordPolicyService,
            ILogger logger)
        {
            _activeDirectoryService = activeDirectoryService;
            _passwordPolicyService = passwordPolicyService;
            _logger = logger;
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

            var validationResult = _passwordPolicyService.ValidatePassword(model.NewPassword);
            if (!validationResult.IsValid)
            {
                _logger.Warning("Unable to change password for user '{u:l}'. Failed to set new password: {err:l}", User.Identity.Name, validationResult);
                ModelState.AddModelError(nameof(model.NewPassword), validationResult.ToString());
                return View(model);
            }
            
            
            if (!_activeDirectoryService.ChangeValidPassword(User.Identity.Name, model.Password, model.NewPassword, out var errorReason))
            {
                ModelState.AddModelError(string.Empty, errorReason);
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}