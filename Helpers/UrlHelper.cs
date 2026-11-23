using System;

namespace MultiFactor.SelfService.Windows.Portal.Helpers
{
    public class UrlHelper
    {
        public static bool TryReadHttpsUrl(string url, out Uri uri)
        {
            uri = null;

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            // https://user:pass@host не наш случай
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                return false;
            }

            return true;
        }
    }
}