using System.Web.Mvc;
using System.Linq;
using System.Web.Routing;

namespace MultiFactor.SelfService.Windows.Portal.Stories
{
    public class RedirectToActionResult
    {
        public RedirectToRouteResult ToActionResult(string actionName, string controllerName, params object[] values)
        {
            var routeValues = new RouteValueDictionary
            {
                { "controller", controllerName },
                { "action", actionName }
            };

            if (values != null)
            {
                foreach (var value in values.Where(v => v != null))
                {
                    var dict = new RouteValueDictionary(value);

                    foreach (var kv in dict)
                    {
                        routeValues[kv.Key] = kv.Value;
                    }
                }
            }

            return new RedirectToRouteResult(routeValues);
        }
    }
}