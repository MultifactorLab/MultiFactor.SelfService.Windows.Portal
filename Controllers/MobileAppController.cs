using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Core;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    public class MobileAppController : ControllerBase
    {
        private readonly JwtTokenProvider _tokenProvider;

        public MobileAppController(JwtTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider ?? throw new System.ArgumentNullException(nameof(tokenProvider));
        }

        public ActionResult Index() => View(_tokenProvider.GetToken());       
    }
}