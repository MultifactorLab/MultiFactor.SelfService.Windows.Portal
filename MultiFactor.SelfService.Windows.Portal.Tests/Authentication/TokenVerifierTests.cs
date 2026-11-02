using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Moq;
using Newtonsoft.Json;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Authentication;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Authentication
{
    public class TokenVerifierTests
    {
        private const string OldPasswordExpirationDate = "10/17/2026 12:00:00";
        private readonly Mock<TokenVerifier> _verifier = new Mock<TokenVerifier>(
            new TokenValidationService(new Configuration(), Mock.Of<ILogger>())) { CallBase = true };

        [Theory]
        [InlineData("saml-session", null, null)]
        [InlineData(null, "oidc-session", null)]
        [InlineData("saml-session", null, OldPasswordExpirationDate)]
        [InlineData(null, "oidc-session", OldPasswordExpirationDate)]
        public void PasswordChangeClaims_PreserveSsoAndCustomClaims(string saml, string oidc, string passwordExpirationDate)
        {
            var payload = new JwtPayload
            {
                ["sub"] = "user@test.local", ["jti"] = "request-id", ["iss"] = "test", ["aud"] = "portal",
                ["iat"] = 1, ["exp"] = 2, ["nbf"] = 1, ["name"] = "Test user", ["email"] = "user@test.local",
                [MultiFactorClaims.ChangePassword] = "true", [MultiFactorClaims.RawUserName] = "jdoe",
                ["amr"] = "pwd", ["department"] = "engineering", ["groups"] = new[] { "a", "b" },
                ["cnf"] = new Dictionary<string, string> { ["jkt"] = "key-thumbprint" }
            };
            if (saml != null) payload[MultiFactorClaims.SamlSessionId] = saml;
            if (oidc != null) payload[MultiFactorClaims.OidcSessionId] = oidc;
            if (passwordExpirationDate != null) payload[MultiFactorClaims.PasswordExpirationDate] = passwordExpirationDate;
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(), payload));
            _verifier.Setup(x => x.Verify(token)).Returns(new TokenClaims
            {
                Identity = "user@test.local", RawUserName = "jdoe", SamlClaim = saml, OidcClaim = oidc
            });

            var claims = _verifier.Object.GetPasswordChangeClaims(token);

            Assert.Equal(saml, claims.TryGetValue(MultiFactorClaims.SamlSessionId, out var samlValue) ? samlValue : null);
            Assert.Equal(oidc, claims.TryGetValue(MultiFactorClaims.OidcSessionId, out var oidcValue) ? oidcValue : null);
            Assert.Equal("true", claims[MultiFactorClaims.AdditionSsoStep]);
            Assert.Equal("jdoe", claims[MultiFactorClaims.RawUserName]);
            Assert.Equal("pwd", claims["amr"]);
            Assert.Equal("engineering", claims["department"]);
            Assert.Equal(new[] { "a", "b" }, JsonConvert.DeserializeObject<string[]>(claims["groups"]));
            Assert.Equal("key-thumbprint", claims["cnf_jkt"]);
            Assert.Equal(passwordExpirationDate,
                claims.TryGetValue(MultiFactorClaims.PasswordExpirationDate, out var expiration) ? expiration : null);
            foreach (var excluded in new[] { "sub", "jti", "iss", "aud", "iat", "exp", "nbf", "name", "email", "cnf", MultiFactorClaims.ChangePassword })
            {
                Assert.False(claims.ContainsKey(excluded), excluded);
            }
            _verifier.Verify(x => x.Verify(token), Times.Once);
        }

        [Fact]
        public void PasswordChangeClaims_InvalidTokenStopsBeforeClaimsAreRead()
        {
            var failure = new InvalidOperationException("invalid token");
            _verifier.Setup(x => x.Verify("invalid-token")).Throws(failure);

            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                _verifier.Object.GetPasswordChangeClaims("invalid-token")));
        }
    }
}
