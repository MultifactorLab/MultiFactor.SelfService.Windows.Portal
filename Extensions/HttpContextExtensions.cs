using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Extensions
{
    public static class HttpContextExtensions
    {
        public static Dictionary<string, string> GetRequiredHeaders(this HttpContext httpContext)
        {
            var headers = httpContext.Request.Headers.AllKeys
                .Where(key => ShouldForwardHeader(key))
                .ToDictionary(
                    key => key,
                    key => httpContext.Request.Headers[key]
                );

            var clientIp = httpContext.Request.UserHostAddress;
            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                headers["X-Original-Client-IP"] = clientIp;
            }

            return headers;
        }

        public static Dictionary<string, string> GetRequiredHeaders(this HttpContextBase httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            var request = httpContext.Request;

            var headers = request.Headers.AllKeys
                .Where(key => ShouldForwardHeader(key))
                .ToDictionary(
                    key => key,
                    key => request.Headers[key]
                );

            var clientIp = request.UserHostAddress;
            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                headers["X-Original-Client-IP"] = clientIp;
            }

            return headers;
        }

        private static bool ShouldForwardHeader(string key)
        {
            return AllowedForwardHeaders.Contains(key);
        }

        private static readonly HashSet<string> AllowedForwardHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "User-Agent",
            "X-Device-Id",
            "X-Device-Type"
        };
    }
}