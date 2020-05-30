using System.Web.Mvc;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.QueryString["samlSessionId"] != null)
            {
                //re-login for saml authentication
                FormsAuthentication.SignOut();
                FormsAuthentication.RedirectToLoginPage();
            }
            
            return View();
        }
    }
}