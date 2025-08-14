using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult Index() => View();
        public ActionResult SessionExpired() => View();
    }
}