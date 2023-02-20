using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    public class TelegramController : ControllerBase
    {
        private readonly JwtTokenProvider _tokenProvider;
        private readonly TokenValidationService _tokenValidationService;

        public TelegramController(JwtTokenProvider tokenProvider, TokenValidationService tokenValidationService)
        {
            _tokenProvider = tokenProvider ?? throw new System.ArgumentNullException(nameof(tokenProvider));
            _tokenValidationService = tokenValidationService ?? throw new System.ArgumentNullException(nameof(tokenValidationService));
        }

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