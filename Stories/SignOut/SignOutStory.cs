using System.Collections.Generic;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using MultiFactor.SelfService.Windows.Portal.ModelBinding.Binders;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignOut
{

    public class SignOutStory
    {
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly IMultifactorIdpApi _idpApi;
        private readonly ILogger _logger;

        public SignOutStory(SafeHttpContextAccessor contextAccessor, IMultifactorIdpApi idpApi, ILogger logger)
        {
            _contextAccessor = contextAccessor;
            _idpApi = idpApi;
            _logger = logger;
        }

        public ActionResult Execute()
        {
            _contextAccessor.HttpContext.Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.Now.AddDays(-1);

            var request = new LogoutRequestDto
            {
                Reason = "logout"
            };

            try
            {
                var headers = _contextAccessor.HttpContext.GetRequiredHeaders();
                var response = _idpApi.LogoutAsync(request, headers).GetAwaiter().GetResult();

                if (!response.Success)
                {
                    _logger.Warning("Logout failed: {Error}", response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during logout");
            }

            var redirectUrl = new StringBuilder("/account/login");
            var claimsDto = MultiFactorClaimsDtoBinder.FromRequest(_contextAccessor.HttpContext.Request);
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.SamlSessionId}={claimsDto.SamlSessionId}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.OidcSessionId}={claimsDto.OidcSessionId}");
            }

            var res = redirectUrl.ToString();

            return new RedirectResult(res, false);
        }

        public async Task<ActionResult> ExecuteAsync(Dictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            _contextAccessor.HttpContext.Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.Now.AddDays(-1);

            var request = new LogoutRequestDto
            {
                Reason = "logout"
            };

            var response = await _idpApi.LogoutAsync(request, headers);

            if (!response.Success)
            {
                _logger.Warning("Logout failed: {Error}", response.ErrorMessage);
            }

            var redirectUrl = new StringBuilder("/account/login");
            var claimsDto = MultiFactorClaimsDtoBinder.FromRequest(_contextAccessor.HttpContext.Request);
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.SamlSessionId}={claimsDto.SamlSessionId}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.OidcSessionId}={claimsDto.OidcSessionId}");
            }

            var res = redirectUrl.ToString();

            return new RedirectResult(res, false);
        }
    }
}