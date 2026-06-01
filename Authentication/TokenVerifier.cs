using System;
using MultiFactor.SelfService.Windows.Portal.Services;

namespace MultiFactor.SelfService.Windows.Portal.Authentication
{
    public class TokenVerifier
    {
        private readonly TokenValidationService _tokenValidationService;

        public TokenVerifier(TokenValidationService tokenValidationService)
        {
            _tokenValidationService = tokenValidationService ?? throw new ArgumentNullException(nameof(tokenValidationService));
        }

        public TokenClaims Verify(string accessToken)
        {
            return _tokenValidationService.Verify(accessToken);
        }
    }
}
