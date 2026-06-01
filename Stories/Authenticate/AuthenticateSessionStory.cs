using System.Threading.Tasks;
using System;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using System.Web;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal.Stories.Authenticate
{
    public class AuthenticateSessionStory
    {
        private readonly IMultifactorIdpApi _idpApi;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly ILogger _logger;

        public AuthenticateSessionStory(
            IMultifactorIdpApi idpApi,
            SafeHttpContextAccessor contextAccessor,
            ILogger logger)
        {
            _idpApi = idpApi;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        public async Task<ActionResult> Execute(string accessToken)
        {
            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }
            _logger.Debug("Received MFA token: {accessToken:l}", accessToken);

            var request = new LoginCompletedRequestDto
            {
                AccessToken = accessToken
            };

            var response = await _idpApi.LoginCompletedAsync(request, _contextAccessor.HttpContext.GetRequiredHeaders());

            return HandleLoginCompletedResponse(response, accessToken);
        }

        private ActionResult HandleLoginCompletedResponse(LoginCompletedResponseDto response, string accessToken)
        {
            if (!response.Success)
            {
                _logger.Debug("LoginCompleted failed: {Error}", response.ErrorMessage);
                return new RedirectToActionResult().ToActionResult("AccessDenied", "Error", default);
            }

            if (response.TokenExpirationDate != null)
            {

                var cookie = new HttpCookie(Constants.COOKIE_NAME)
                {
                    Value = accessToken,
                    Expires = response.TokenExpirationDate,
                    HttpOnly = true,
                    Secure = true
                };

                if (HttpContext.Current.Response.Cookies[Constants.COOKIE_NAME] != null)
                {
                    HttpContext.Current.Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.Now.AddDays(-1);
                }
                HttpContext.Current.Response.Cookies.Add(cookie);
                FormsAuthentication.SetAuthCookie(response.Identity, false);

                _logger.Information("cookie set success " + response.Identity);
            }

            if (response.Action == LoginCompletedAction.BypassSaml && !string.IsNullOrEmpty(response.SamlSessionId))
            {
                _logger.Debug("Redirecting to SAML bypass for session '{Session}'", response.SamlSessionId);
                return new RedirectToActionResult().ToActionResult("ByPassSamlSession", "Account", new { samlSession = response.SamlSessionId });
            }

            if (response.Action == LoginCompletedAction.BypassOidc && !string.IsNullOrEmpty(response.OidcSessionId))
            {
                _logger.Debug("Redirecting to OIDC bypass for session '{Session}'", response.OidcSessionId);
                return new RedirectToActionResult().ToActionResult("ByPassOidcSession", "Account", new { oidcSession = response.OidcSessionId });
            }

            if (response.Action == LoginCompletedAction.ChangePassword)
            {
                _logger.Debug("User '{User}' must change password", response.Identity);
                return new RedirectToActionResult().ToActionResult("Change", "ExpiredPassword", default);
            }

            _logger.Debug("User '{User}' authenticated successfully", response.Identity);
            return new RedirectToActionResult().ToActionResult("Index", "Home", default);
        }
    }
}