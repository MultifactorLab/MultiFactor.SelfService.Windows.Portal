using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Models.PasswordRecovery;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using Serilog;
using System;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [AllowAnonymous]
    [RequiredFeature(ApplicationFeature.PasswordManagement | ApplicationFeature.Captcha)]
    public class ForgottenPasswordController : ControllerBase
    {
        private readonly MultiFactorSelfServiceApiClient _apiClient;
        private readonly ILogger _logger;
        private readonly ActiveDirectoryService _activeDirectory;
        private readonly TokenValidationService _tokenValidationService;
        private readonly DataProtectionService _dataProtectionService;

        public ForgottenPasswordController(MultiFactorSelfServiceApiClient apiClient,
            ILogger logger,
            ActiveDirectoryService activeDirectory,
            TokenValidationService tokenValidationService,
            DataProtectionService dataProtectionService)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeDirectory = activeDirectory ?? throw new ArgumentNullException(nameof(activeDirectory));
            _tokenValidationService = tokenValidationService ?? throw new ArgumentNullException(nameof(tokenValidationService));
            _dataProtectionService = dataProtectionService ?? throw new ArgumentNullException(nameof(dataProtectionService));
        }

        [HttpGet]
        public ActionResult Index() => View();

        [HttpPost]
        [VerifyCaptcha]
        [ValidateAntiForgeryToken]
        public ActionResult Index(EnterIdentityForm form)
        {
            if (!ModelState.IsValid)
            {
                return View(form);
            }

            if (Configuration.Current.RequiresUpn)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(form.Identity);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    ModelState.AddModelError(string.Empty, Resources.AccountLogin.UserNameUpnRequired);
                    return View(form);
                }
            }

            var callback = CallbackUrlFactory.BuildCallbackUrl(form.MyUrl, "reset");
            try
            {
                var adValidationResult = _activeDirectory.VerifyMembership(LdapIdentity.ParseUser(form.Identity));
                var identity = adValidationResult.GetIdentityForPasswordRecovery(form.Identity);
                var response = _apiClient.StartResetPassword(identity, callback);
                if (response.Success) return RedirectPermanent(response.Model.Url);

                _logger.Error("Unable to recover password for user '{u:l}': {m:l}", identity, response.Message);
                TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                return RedirectToAction("Wrong");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                return RedirectToAction("Wrong");
            }
        }

        [HttpPost]
        public ActionResult Reset(string accessToken)
        {
            if (!_tokenValidationService.VerifyToken(accessToken, out var token))
            {
                _logger.Error("Invalid reset password session: access token verification error");
                TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                return RedirectToAction("Wrong");
            }

            if (!token.MustResetPassword)
            {
                _logger.Error("Invalid reset password session for user '{identity:l}': required claims not found", token.Identity);
                TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                return RedirectToAction("Wrong");
            }

            var cookie = new HttpCookie(Constants.PWD_RECOVERY_COOKIE)
            {
                Value = _dataProtectionService.Protect(token.Identity),
                Expires = DateTime.Now.AddMinutes(5),
                Path = "/",
                Secure = true,
                HttpOnly = true
            };
            if (Response.Cookies[Constants.PWD_RECOVERY_COOKIE] != null)
            {
                Response.Cookies.Remove(Constants.PWD_RECOVERY_COOKIE);
            }
            Response.Cookies.Add(cookie);

            return View(new ResetPasswordForm
            {
                Identity = token.Identity
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmReset(ResetPasswordForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Identity))
            {
                _logger.Error("Invalid reset password form state: empty identity");
            }

            if (!ModelState.IsValid)
            {
                return View("Reset", form);
            }

            var sesionCookie = Request.Cookies[Constants.PWD_RECOVERY_COOKIE]?.Value;
            if (sesionCookie == null || _dataProtectionService.Unprotect(sesionCookie) != form.Identity)
            {
                _logger.Error("Invalid reset password session for user '{identity:l}': session not found", form.Identity);
                ModelState.AddModelError(string.Empty, Resources.PasswordReset.Fail);
                return View("Reset", form);
            }

            if (!_activeDirectory.ResetPassword(form.Identity, form.NewPassword, out var errorReason))
            {
                _logger.Error("Unable to reset password for identity '{id:l}'. Failed to set new password: {err:l}", form.Identity, errorReason);
                ModelState.AddModelError(string.Empty, Resources.PasswordReset.Fail);
                return View("Reset", form);
            }

            return RedirectToAction("Done");
        }

        public ActionResult Wrong()
        {
            var error = TempData["reset-password-error"] ?? Resources.PasswordReset.ErrorMessage;
            return View(error);
        }

        public ActionResult Done()
        {
            if (Response.Cookies[Constants.PWD_RECOVERY_COOKIE] != null)
            {
                Response.Cookies.Remove(Constants.PWD_RECOVERY_COOKIE);
            }
            return View();
        }
    }
}