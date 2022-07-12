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
        private ILogger _logger = Log.Logger;
        private Configuration _configuration = Configuration.Current;

        //cached jwks
        private static JsonWebKeySet _jsonWebKeySet;

        public bool VerifyToken(string jwt, out Token token)
        {
            token = null;

            try
            {
                if (_jsonWebKeySet == null)
                {
                    FetchJwks();
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

                //use raw user name when possible couse multifactor may transform identity depend by settings

                token = new Token
                {
                    Id = jwtSecurityToken.Id,
                    Identity = rawUserName ?? identity,
                    MustChangePassword = claimsPrincipal.Claims.Any(claim => claim.Type == MultiFactorClaims.ChangePassword),
                    ValidTo = jwtSecurityToken.ValidTo
                };

                return true; //token valid
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error verifying token");
            }

            return false;
        }

        private void FetchJwks()
        {
            //load Json Web Key Set from MultiFactor API
            //JWKS used for signature validation

            var jwksUrl = _configuration.MultiFactorApiUrl + "/.well-known/jwks.json";

            try
            {
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

                    _jsonWebKeySet = new JsonWebKeySet(json);
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