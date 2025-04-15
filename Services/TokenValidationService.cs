using Microsoft.IdentityModel.Tokens;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using Serilog;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    /// <summary>
    /// Service to load public key and verify token signature, issuer and expiration date
    /// </summary>
    public class TokenValidationService
    {
        //cached jwks
        private static JsonWebKeySet _jsonWebKeySet;
        private readonly Configuration _configuration;
        private readonly ILogger _logger;

        public TokenValidationService(Configuration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool VerifyToken(string jwt, out Token token)
        {
            token = null;

            try
            {
                if (_jsonWebKeySet == null)
                {
                    _jsonWebKeySet = FetchJwks();
                }

                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKeys = _jsonWebKeySet.Keys,
                    ValidAudience = _configuration.MultiFactorApiKey,
                    ValidateIssuer = false,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateTokenReplay = true,
                };

                var handler = new JwtSecurityTokenHandler();
                var claimsPrincipal = handler.ValidateToken(jwt, validationParameters, out var securityToken);

                var jwtSecurityToken = (JwtSecurityToken)securityToken;

                var identity = jwtSecurityToken.Subject;
                var rawUserName = claimsPrincipal.Claims.SingleOrDefault(claim => claim.Type == MultiFactorClaims.RawUserName)?.Value;
                var unlockUser = claimsPrincipal.Claims.FirstOrDefault(claim => claim.Type == MultiFactorClaims.UnlockUser)?.Value?.ToLower() == "true";
                token = new Token
                {
                    Id = jwtSecurityToken.Id,
                    Identity = rawUserName ?? identity,
                    MustChangePassword = claimsPrincipal.Claims.Any(claim => claim.Type == MultiFactorClaims.ChangePassword),
                    MustResetPassword = claimsPrincipal.Claims.Any(claim => claim.Type == MultiFactorClaims.ResetPassword),
                    ValidTo = jwtSecurityToken.ValidTo,
                    MustUnlockUser = unlockUser
                };

                var passwordExpirationDate = claimsPrincipal.Claims.FirstOrDefault(claim => claim.Type == MultiFactorClaims.PasswordExpirationDate);
                if (_configuration.NotifyOnPasswordExpirationDaysLeft > 0 && passwordExpirationDate?.Value != null)
                {
                    token.PasswordExpirationDate = DateTime.Parse(passwordExpirationDate.Value);
                }

                return true; //token valid
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error verifying token");
            }

            return false;
        }

        private JsonWebKeySet FetchJwks()
        {
            //load Json Web Key Set from MultiFactor API
            //JWKS used for signature validation

            var jwksUrl = _configuration.MultiFactorApiUrl + "/.well-known/jwks.json";

            try
            {
                // TODO: httpClient
                using (var web = new WebClient())
                {
                    _logger.Debug($"Fetching jwks from {jwksUrl}");

                    if (!string.IsNullOrEmpty(_configuration.MultiFactorApiProxy))
                    {
                        _logger.Debug("Using proxy " + _configuration.MultiFactorApiProxy);
                        var proxyUri = new Uri(_configuration.MultiFactorApiProxy);
                        web.Proxy = new WebProxy(proxyUri);

                        if (!string.IsNullOrEmpty(proxyUri.UserInfo))
                        {
                            var credentials = proxyUri.UserInfo.Split(new[] { ':' }, 2);
                            web.Proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
                        }
                    }

                    var json = web.DownloadString(jwksUrl);
                    _logger.Debug($"Fetched jwks\n{json}");

                    return new JsonWebKeySet(json);
                }
            }
            catch
            {
                _logger.Error($"Unable to fetch jwks from {jwksUrl}");
                throw;
            }
        }
    }
}