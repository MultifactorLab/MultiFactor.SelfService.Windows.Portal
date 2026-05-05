using System.Collections.Generic;
using System.Globalization;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignIn.ClaimsSources
{
    public class MultiFactorClaimsSource : IClaimsSource
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;

        public MultiFactorClaimsSource(SafeHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var result = _httpContextAccessor.SafeGetCredVerificationResult();
            var claims = new Dictionary<string, string>
            {
                { MultiFactorClaims.RawUserName, result.Username }
            };

            if (result.UserMustChangePassword)
            {
                claims.Add(MultiFactorClaims.ChangePassword, "true");
                return claims;
            }

            claims.Add(MultiFactorClaims.PasswordExpirationDate,
                result.PasswordExpirationDate.ToString(CultureInfo.InvariantCulture));

            return claims;
        }
    }
}