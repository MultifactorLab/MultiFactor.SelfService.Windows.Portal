using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Models.PasswordRecovery;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using Serilog;
using System;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [AllowAnonymous]
    [RequiredFeature(ApplicationFeature.PasswordManagement | ApplicationFeature.Captcha)]
    public class ForgottenPasswordController : ControllerBase
    {
        private readonly MultiFactorSelfServiceApiClient _apiClient;
        private readonly ILogger _logger;

        public ForgottenPasswordController(MultiFactorSelfServiceApiClient apiClient, ILogger logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        [VerifyCaptcha]
        [ValidateAntiForgeryToken]
        public ActionResult Index(EnterIdentityForm form)
        {
            var callback = HttpContext.BuildCallbackUrl($"reset?identity={form.Identity}");
            try
            {
                var response = _apiClient.StartResetPassword(form.Identity, callback);
                if (response.Success) return RedirectPermanent(response.Model.Url);

                _logger.Error("Unable to recover password for user '{u:l}': {m:l}", form.Identity, response.Message);
                TempData["reset-password-error"] = response.Message;
                return RedirectToAction("Wrong");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                return RedirectToAction("Wrong");
            }

        }

        [HttpGet]
        public ActionResult Reset(string identity)
        {
            return View(new ResetPasswordForm 
            { 
                Identity = identity
            });
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reset(ResetPasswordForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Identity))
            {
                _logger.Error("Invalid reset password form state: empty identity");
            }

            if (!ModelState.IsValid)
            {
                return View(form);
            }

            return View(form);
        }

        public ActionResult Wrong()
        {
            var error = TempData["reset-password-error"] ?? Resources.PasswordReset.ErrorMessage;
            return View(error);
        }
    }
}