using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Services;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ScopeInfoService _scopeInfoService;
        public ErrorController(ScopeInfoService scopeInfoService)
        {
            _scopeInfoService = scopeInfoService;
        }
        
        public ActionResult Index() => View();
        public ActionResult SessionExpired() => View();
        
        public ActionResult AccessDenied()
        {
            var adminInfo = _scopeInfoService.GetSupportInfo();
            return View(adminInfo);
        }
    }
}