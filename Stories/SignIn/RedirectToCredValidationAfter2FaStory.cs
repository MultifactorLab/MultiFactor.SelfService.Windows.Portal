using System.Threading.Tasks;
using System;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;
using System.IdentityModel.Tokens.Jwt;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Models;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignIn
{
    public class RedirectToCredValidationAfter2FaStory
    {
        private readonly ILogger _logger;
        private readonly IApplicationCache _applicationCache;
        private readonly IMultifactorIdpApi _idpApi;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly AuthenticateSessionStory _authenticateSessionStory;

        public RedirectToCredValidationAfter2FaStory(
            IApplicationCache applicationCache,
            ILogger logger,
            IMultifactorIdpApi idpApi,
            AuthenticateSessionStory authenticateSessionStory,
            SafeHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _applicationCache = applicationCache;
            _idpApi = idpApi;
            _authenticateSessionStory = authenticateSessionStory;
            _contextAccessor = contextAccessor;
        }

        public async Task<ActionResult> ExecuteAsync(string accessToken)
        {
            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }
            _logger.Debug("Extracting token information for PreAuthenticationMethod flow");

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken token;
            try
            {
                token = handler.ReadJwtToken(accessToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to parse access token");
                return new RedirectToActionResult().ToActionResult("Login", "Account", null);
            }

            var requestId = token.Id;
            if (string.IsNullOrEmpty(requestId))
            {
                _logger.Error("Token ID is missing");
                return new RedirectToActionResult().ToActionResult("Login", "Account", null);
            }

            var request = new LoginCompletedRequestDto
            {
                AccessToken = accessToken
            };

            var authCacheResult = _applicationCache.GetPreauthenticationAuthn(ApplicationCacheKeyFactory.CreatePreAuthenticationAuthnSucceedKey(token.Subject));
            if (!authCacheResult.IsEmpty && authCacheResult.Value)
            {
                _applicationCache.Remove(ApplicationCacheKeyFactory.CreatePreAuthenticationAuthnSucceedKey(token.Subject));
                return await _authenticateSessionStory.Execute(accessToken);
            }

            try
            {
                var response = await _idpApi.LoginCompletedAsync(request, _contextAccessor.HttpContext.GetRequiredHeaders());

                if (!response.Success)
                {
                    _logger.Warning("LoginCompleted failed after pre-auth MFA: {Error}", response.ErrorMessage);
                    return new RedirectToActionResult().ToActionResult("AccessDenied", "Error", null);
                }

                var username = !string.IsNullOrWhiteSpace(response.RawUserName)
                    ? response.RawUserName
                    : response.Identity;

                if (string.IsNullOrEmpty(username))
                {
                    _logger.Error("Can't determine username from token");
                    return new RedirectToActionResult().ToActionResult("Login", "Account", null);
                }

                var cachedModel = _applicationCache.GetPreauthenticationIdentity(ApplicationCacheKeyFactory.CreatePreAuthenticationIdentityKey(username));
                var identityModel = !cachedModel.IsEmpty
                    ? cachedModel.Value
                    : new IdentityModel();
                _applicationCache.Remove(ApplicationCacheKeyFactory.CreatePreAuthenticationIdentityKey(username));

                identityModel.UserName = username;
                identityModel.AccessToken = accessToken;
                _applicationCache.SetIdentity(requestId, identityModel);

                object routeValue = new { requestId = requestId };

                if (!string.IsNullOrEmpty(response.SamlSessionId))
                {
                    _logger.Debug("SAML session found, redirecting to Identity with SAML session");
                    routeValue = new { samlSessionId = response.SamlSessionId, requestId = requestId };
                    return new RedirectToActionResult().ToActionResult("Identity", "Account", routeValue);
                }

                if (!string.IsNullOrEmpty(response.OidcSessionId))
                {
                    _logger.Debug("OIDC session found, redirecting to Identity with OIDC session");
                    routeValue = new { oidcSessionId = response.OidcSessionId, requestId = requestId };
                    return new RedirectToActionResult().ToActionResult("Identity", "Account", routeValue);
                }

                _logger.Debug("Redirecting to Identity page for password entry");
                return new RedirectToActionResult().ToActionResult("Identity", "Account", routeValue);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to extract token information");
                return new RedirectToActionResult().ToActionResult("Login", "Account", null);
            }
        }
    }
}