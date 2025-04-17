using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Services;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [AllowAnonymous]
    [RequiredFeature(ApplicationFeature.PasswordManagement | ApplicationFeature.Captcha)]
    public class UnlockController : ControllerBase
    {
        private readonly TokenValidationService _tokenValidationService;
        private readonly  ActiveDirectoryService _activeDirectoryService;
        private readonly ILogger _logger;

        public UnlockController(TokenValidationService tokenVerifier, ActiveDirectoryService activeDirectoryService, ILogger logger)
        {
            _tokenValidationService = tokenVerifier;
            _logger = logger;
            _activeDirectoryService = activeDirectoryService;
        }
        
        [HttpPost]
        public ActionResult Complete(string accessToken)
        {
            if (!_tokenValidationService.VerifyToken(accessToken, out var token))
            {
                _logger.Error("Invalid unlocking session: access token verification error");
                return RedirectToAction("Wrong");
            }
            
            if (!token.MustUnlockUser)
            {
                _logger.Error("Invalid unlocking session for user '{identity:l}': required claims not found",
                    token.Identity);
                return RedirectToAction("Wrong");
            }

            if (!_activeDirectoryService.UnlockUser(token.Identity))
            {
                return RedirectToAction("Wrong");
            }

            return RedirectToAction("Success");
        }
        
        public ActionResult Wrong()
        {
            return View();
        }

        public ActionResult Success()
        {
            return View();
        }
    }
}