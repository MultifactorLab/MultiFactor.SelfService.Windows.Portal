using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;

namespace MultiFactor.SelfService.Windows.Portal.Authentication
{
    public class TokenVerifier
    {
        private readonly TokenValidationService _tokenValidationService;

        public TokenVerifier(TokenValidationService tokenValidationService)
        {
            _tokenValidationService = tokenValidationService ?? throw new ArgumentNullException(nameof(tokenValidationService));
        }

        public virtual TokenClaims Verify(string accessToken)
        {
            return _tokenValidationService.Verify(accessToken);
        }

        public Dictionary<string, string> GetPasswordChangeClaims(string accessToken)
        {
            var verified = Verify(accessToken);
            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var payload = JObject.Parse(token.Payload.SerializeToJson());
            var excluded = new HashSet<string>(StringComparer.Ordinal)
            {
                "iss", "aud", "sub", "jti", "exp", "iat", "nbf", "cnf", "name", "email",
                MultiFactorClaims.ChangePassword
            };
            var claims = payload.Properties().Where(property => !excluded.Contains(property.Name))
                .ToDictionary(property => property.Name, property => property.Value.Type == JTokenType.String
                    ? (string)property.Value
                    : property.Value.ToString(Formatting.None));

            if (payload.TryGetValue("cnf", out var confirmation))
            {
                claims["cnf_jkt"] = (string)confirmation["jkt"]
                    ?? throw new InvalidOperationException("Token binding is missing.");
            }

            claims[MultiFactorClaims.RawUserName] = verified.RawUserName ?? verified.Identity;
            if (!string.IsNullOrEmpty(verified.SamlClaim) || !string.IsNullOrEmpty(verified.OidcClaim))
            {
                claims[MultiFactorClaims.AdditionSsoStep] = "true";
            }

            return claims;
        }
    }
}
