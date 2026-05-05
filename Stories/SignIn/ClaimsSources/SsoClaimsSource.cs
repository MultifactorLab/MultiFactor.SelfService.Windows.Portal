using System;
using System.Collections.Generic;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignIn.ClaimsSources
{
    public class SsoClaimsSource : IClaimsSource
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;
        private readonly Configuration _portalSettings;

        public SsoClaimsSource(SafeHttpContextAccessor httpContextAccessor, Configuration portalSettings)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _portalSettings = portalSettings;
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var sso = _httpContextAccessor.SafeGetSsoClaims();
            var claims = new Dictionary<string, string>();

            if (sso.HasSamlSession())
            {
                claims.Add(MultiFactorClaims.SamlSessionId, sso.SamlSessionId);
                claims.Add(MultiFactorClaims.AdditionSsoStep, "true");
            }

            if (sso.HasOidcSession())
            {
                claims.Add(MultiFactorClaims.OidcSessionId, sso.OidcSessionId);
                claims.Add(MultiFactorClaims.AdditionSsoStep, "true");
            }

            return claims;
        }
    }
}