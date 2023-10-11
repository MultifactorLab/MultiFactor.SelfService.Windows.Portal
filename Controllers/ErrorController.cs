using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult Index()
        {
            TempData["ErrorMessage"] = (string)HttpContext.Session["ErrorMessage"];
            HttpContext.Session.Remove("ErrorMessage");
            return View();
        }
    }
}