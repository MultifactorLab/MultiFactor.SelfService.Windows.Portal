using System.IO;
using System.Web;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using MultiFactor.SelfService.Windows.Portal.Models;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Extensions
{
    public class SafeHttpContextAccessorExtensionsTests
    {
        [Fact]
        public void SafeGetSsoClaims_LoadsSsoSessionsFromCurrentRequest()
        {
            var previousContext = HttpContext.Current;
            try
            {
                HttpContext.Current = CreateContext("samlSessionId=saml-session&oidcSessionId=oidc-session");

                var result = new SafeHttpContextAccessor().SafeGetSsoClaims();

                Assert.Equal("saml-session", result.SamlSessionId);
                Assert.Equal("oidc-session", result.OidcSessionId);
            }
            finally
            {
                HttpContext.Current = previousContext;
            }
        }

        [Fact]
        public void SafeGetSsoClaims_ReturnsCachedSsoSessions()
        {
            var previousContext = HttpContext.Current;
            try
            {
                HttpContext.Current = CreateContext("samlSessionId=request-session");
                var cached = new SingleSignOnDto { SamlSessionId = "cached-session" };
                HttpContext.Current.Items[Constants.SsoClaims] = cached;

                var result = new SafeHttpContextAccessor().SafeGetSsoClaims();

                Assert.Same(cached, result);
            }
            finally
            {
                HttpContext.Current = previousContext;
            }
        }

        private static HttpContext CreateContext(string queryString)
        {
            var request = new HttpRequest(string.Empty, "https://ssp.example/account/login", queryString);
            return new HttpContext(request, new HttpResponse(TextWriter.Null));
        }
    }
}
