using MultiFactor.SelfService.Windows.Portal.Services.API;
using System;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, Location = OutputCacheLocation.None)]
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

            var returnUrl = FormsAuthentication.LoginUrl;
            var samlSessionId = Request.QueryString[MultiFactorClaims.SamlSessionId];
            if (samlSessionId != null)
            {
                returnUrl += $"?{MultiFactorClaims.SamlSessionId}={samlSessionId}";
            }
            
            var oidcSessionId = Request.QueryString[MultiFactorClaims.OidcSessionId];
            if (oidcSessionId != null)
            {
                returnUrl += $"?{MultiFactorClaims.OidcSessionId}={oidcSessionId}";
            }

            return Redirect(returnUrl);
        }
    }
}