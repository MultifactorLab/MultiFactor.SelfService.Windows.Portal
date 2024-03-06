using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Services;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MultiFactor.SelfService.Windows.Portal.Attributes
{
    /// <summary>
    /// Specifies that access to a controller or action method is restricted to users 
    /// who meet the authorization requirement.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class IsAuthorizedAttribute : AuthorizeAttribute
    {
        private bool _autorizeСore;
        private readonly bool _validateUserSession;

        /// <summary>
        /// Creates instance of attribute.
        /// </summary>
        /// <param name="validateUserSession">true: validate cookies, JWT claims and that user is authenticated. false: validate that user is authenticated only.</param>
        public IsAuthorizedAttribute(bool validateUserSession = true)
        {
            _validateUserSession = validateUserSession;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (!_validateUserSession || !_autorizeСore) return;

            var scope = (IServiceScope)filterContext.HttpContext.Items[typeof(IServiceScope)];
            var validationSrv = scope.ServiceProvider.GetRequiredService<TokenValidationService>();
            var cookie = filterContext.HttpContext.Request.Cookies[Constants.COOKIE_NAME];
            if (cookie == null || !validationSrv.VerifyToken(cookie.Value, out var token))
            {
                HandleUnauthorizedRequest(filterContext);
                return;
            }

            if (token.MustChangePassword)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "action", "Change" },
                    { "controller", "ExpiredPassword" }
                });
            }

            if(token.PasswordExpirationDate != null)
            {
                filterContext.RequestContext.HttpContext.Items.Add("passwordExpirationDate", token.PasswordExpirationDate);
			}
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var authorized = base.AuthorizeCore(httpContext);
            if (!_validateUserSession)
            {
                _autorizeСore = authorized;
                return _autorizeСore;
            }

            var hasCookie = httpContext.Request.Cookies[Constants.COOKIE_NAME] != null;
            _autorizeСore = authorized && hasCookie;
            return _autorizeСore;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var returnUrl = AppAuthentication.SignOut();
            filterContext.Result = new RedirectResult(returnUrl);
        }
    }
}