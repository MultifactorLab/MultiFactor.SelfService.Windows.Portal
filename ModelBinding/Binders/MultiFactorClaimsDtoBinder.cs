using System;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;
using System.Web;
using MultiFactor.SelfService.Windows.Portal.Models;

namespace MultiFactor.SelfService.Windows.Portal.ModelBinding.Binders
{
    public static class MultiFactorClaimsDtoBinder
    {
        public static SingleSignOnDto FromRequest(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var saml = request[MultiFactorClaims.SamlSessionId];
            var oidc = request[MultiFactorClaims.OidcSessionId];

            var sso = new SingleSignOnDto();
            sso.SamlSessionId = saml ?? string.Empty;
            sso.OidcSessionId = oidc ?? string.Empty;

            return sso;
        }
    }
}
