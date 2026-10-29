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
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Authentication;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;

namespace MultiFactor.SelfService.Windows.Portal.Stories.Authenticate
{
    public class AuthenticateSessionStory
    {
        private readonly IMultifactorIdpApi _idpApi;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly ILogger _logger;
        private readonly Configuration _settings;
        private readonly MultiFactorApiClient _api;
        private readonly TokenVerifier _tokenVerifier;

        public AuthenticateSessionStory(
            IMultifactorIdpApi idpApi,
            SafeHttpContextAccessor contextAccessor,
            ILogger logger,
            Configuration settings,
            MultiFactorApiClient api,
            TokenVerifier tokenVerifier)
        {
            _idpApi = idpApi;
            _contextAccessor = contextAccessor;
            _logger = logger;
            _settings = settings;
            _api = api;
            _tokenVerifier = tokenVerifier;
        }

        public Task<ActionResult> Execute(string accessToken)
            => ExecuteAsync(accessToken, false, new RedirectToActionResult().ToActionResult("Index", "Home", default),
                new RedirectToActionResult().ToActionResult("AccessDenied", "Error", default));

        public async Task<ActionResult> ExecutePasswordChangeAsync(string accessToken)
        {
            TokenClaims original = null;
            try
            {
                original = _tokenVerifier.Verify(accessToken);
                if (!original.MustChangePassword)
                {
                    var claims = _tokenVerifier.GetPasswordChangeClaims(accessToken);
                    claims[MultiFactorClaims.ChangePassword] = "true";
                    var refreshedToken = _api.RefreshAccessToken(accessToken, claims);
                    var refreshed = _tokenVerifier.Verify(refreshedToken);
                    if (!refreshed.MustChangePassword
                        || refreshed.Identity != original.Identity
                        || refreshed.Id != original.Id
                        || refreshed.RawUserName != (original.RawUserName ?? original.Identity)
                        || refreshed.SamlClaim != original.SamlClaim
                        || refreshed.OidcClaim != original.OidcClaim)
                    {
                        throw new InvalidOperationException("Refreshed token has unexpected claims.");
                    }

                    accessToken = refreshedToken;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Unable to prepare mandatory password change");
                AppAuthentication.SignOut();
                return new RedirectToActionResult().ToActionResult("Login", "Account",
                    new { samlSessionId = original?.SamlClaim, oidcSessionId = original?.OidcClaim });
            }

            return await ExecuteAsync(accessToken, true, new RedirectToActionResult().ToActionResult("Index", "Home", default),
                new RedirectToActionResult().ToActionResult("AccessDenied", "Error", default));
        }

        public Task<ActionResult> ExecuteAfterPasswordChangeAsync(string accessToken, string samlSessionId = null, string oidcSessionId = null)
            => ExecuteAsync(accessToken, false, new RedirectToActionResult().ToActionResult("Done", "ExpiredPassword", default),
                new RedirectToActionResult().ToActionResult("Login", "Account", new { samlSessionId, oidcSessionId }));

        private async Task<ActionResult> ExecuteAsync(string accessToken, bool forcePasswordChange,
            ActionResult authenticatedResult, ActionResult errorResult)
        {
            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }
            _logger.Debug("Received MFA token");

            var request = new LoginCompletedRequestDto
            {
                AccessToken = accessToken
            };

            var response = await _idpApi.LoginCompletedAsync(request, _contextAccessor.HttpContext.GetRequiredHeaders());

            return HandleLoginCompletedResponse(response, accessToken, forcePasswordChange, authenticatedResult, errorResult);
        }

        private ActionResult HandleLoginCompletedResponse(LoginCompletedResponseDto response, string accessToken,
            bool forcePasswordChange, ActionResult authenticatedResult, ActionResult errorResult)
        {
            if (!response.Success)
            {
                _logger.Debug("LoginCompleted failed: {Error}", response.ErrorMessage);
                return response.Action == LoginCompletedAction.Error
                    ? errorResult
                    : new RedirectToActionResult().ToActionResult("AccessDenied", "Error", default);
            }

            var passwordChangeRequired = forcePasswordChange || response.Action == LoginCompletedAction.ChangePassword;
            if (passwordChangeRequired && !_settings.EnablePasswordManagement)
            {
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

            if (passwordChangeRequired)
            {
                _logger.Debug("User '{User}' must change password", response.Identity);
                return new RedirectToActionResult().ToActionResult("Change", "ExpiredPassword", default);
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

            _logger.Debug("User '{User}' authenticated successfully", response.Identity);
            return authenticatedResult;
        }
    }
}
