using System;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Core.Http;

namespace MultiFactor.SelfService.Windows.Portal.Authentication
{
    public class TokenClaimsAccessor
    {
        private readonly TokenVerifier _tokenVerifier;
        private readonly SafeHttpContextAccessor _contextAccessor;

        public TokenClaimsAccessor(TokenVerifier tokenVerifier, SafeHttpContextAccessor contextAccessor)
        {
            _tokenVerifier = tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        public TokenClaims GetTokenClaims()
        {
            var token = ExtractBearerToken(_contextAccessor.HttpContext.Request.Headers["Authorization"]);
            return _tokenVerifier.Verify(token);
        }

        private static string ExtractBearerToken(string headerValue)
        {
            if (headerValue is null) throw new UnauthorizedException("Empty token");

            const string bearer = "Bearer";
            if (!headerValue.StartsWith(bearer)) throw new UnauthorizedException("Invalid token");

            return headerValue.Replace(bearer, "").Trim();
        }
    }
}
