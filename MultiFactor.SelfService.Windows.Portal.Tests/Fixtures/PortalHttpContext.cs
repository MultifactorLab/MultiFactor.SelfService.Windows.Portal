using System;
using System.Collections;
using System.IO;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Fixtures
{
    internal sealed class PortalHttpContext : IDisposable
    {
        private readonly HttpContext _previous = HttpContext.Current;

        public PortalHttpContext()
        {
            HttpContext.Current = new HttpContext(
                new HttpRequest(string.Empty, "https://ssp.example/", string.Empty),
                new HttpResponse(TextWriter.Null));
            HttpContext.Current.Request.Browser = new HttpBrowserCapabilities
            {
                Capabilities = new Hashtable { ["cookies"] = "true", ["supportsEmptyStringInCookieValue"] = "true" }
            };
        }

        public void Dispose() => HttpContext.Current = _previous;
    }
}
