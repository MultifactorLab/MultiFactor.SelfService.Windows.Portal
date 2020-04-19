using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}