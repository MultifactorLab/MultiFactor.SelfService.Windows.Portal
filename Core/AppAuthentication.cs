using MultiFactor.SelfService.Windows.Portal.Services.API;
using System;
using System.Web;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public static class AppAuthentication
    {
        public static string SignOut()
        {
            FormsAuthentication.SignOut();

            //remove mfa cookie
            if (HttpContext.Current.Request.Cookies[Constants.COOKIE_NAME] != null)
            {
                HttpContext.Current.Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.Now.AddDays(-1);
            }

            var returnUrl = FormsAuthentication.LoginUrl;
            var samlSessionId = HttpContext.Current.Request.QueryString[MultiFactorClaims.SamlSessionId];
            if (samlSessionId != null)
            {
                returnUrl += $"?{MultiFactorClaims.SamlSessionId}={samlSessionId}";
            }

            var oidcSessionId = HttpContext.Current.Request.QueryString[MultiFactorClaims.OidcSessionId];
            if (oidcSessionId != null)
            {
                returnUrl += $"?{MultiFactorClaims.OidcSessionId}={oidcSessionId}";
            }

            return returnUrl;
        }
    }
}