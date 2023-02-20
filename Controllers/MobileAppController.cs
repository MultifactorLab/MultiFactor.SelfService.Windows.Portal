using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    public class MobileAppController : ControllerBase
    {
        private readonly JwtTokenProvider _tokenProvider;
        private readonly TokenValidationService _tokenValidationService;

        public MobileAppController(JwtTokenProvider tokenProvider, TokenValidationService tokenValidationService)
        {
            _tokenProvider = tokenProvider ?? throw new System.ArgumentNullException(nameof(tokenProvider));
            _tokenValidationService = tokenValidationService ?? throw new System.ArgumentNullException(nameof(tokenValidationService));
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (_tokenValidationService.VerifyToken(_tokenProvider.GetToken(), out var parsedToken))
            {
                return View(parsedToken);
            }
            return SignOut();
        }
    }
}