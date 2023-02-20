using MultiFactor.SelfService.Windows.Portal.Core;
using System.Web.Mvc;
using System.Web.UI;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, Location = OutputCacheLocation.None)]
    public abstract class ControllerBase : Controller
    {
        protected ActionResult SignOut()
        {
            var returnUrl = AppAuthentication.SignOut();
            return Redirect(returnUrl);
        }
    }
}