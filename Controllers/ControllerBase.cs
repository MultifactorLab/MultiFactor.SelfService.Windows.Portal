using System;
using System.Web.Mvc;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0)]
    public abstract class ControllerBase : Controller
    {
        protected ActionResult SignOut()
        {
            FormsAuthentication.SignOut();

            //remove mfa cookie
            if (Request.Cookies[Constants.COOKIE_NAME] != null)
            {
                Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.Now.AddDays(-1);
            }

            return Redirect(FormsAuthentication.LoginUrl);
        }
    }
}