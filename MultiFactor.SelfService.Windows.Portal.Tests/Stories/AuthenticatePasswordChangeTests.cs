using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Moq.AutoMock;
using MultiFactor.SelfService.Windows.Portal.Authentication;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Stories.SignIn;
using MultiFactor.SelfService.Windows.Portal.Tests.Fixtures;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Stories
{
    public class AuthenticatePasswordChangeTests : IDisposable
    {
        private readonly AutoMocker _mocker = new AutoMocker();
        private readonly PortalHttpContext _context = new PortalHttpContext();
        private readonly AuthenticateSessionStory _story;

        public AuthenticatePasswordChangeTests()
        {
            var settings = new Configuration();
            typeof(Configuration).GetProperty(nameof(Configuration.EnablePasswordManagement)).SetValue(settings, true);
            _mocker.Use(settings);
            _mocker.Use(new SafeHttpContextAccessor());
            _mocker.GetMock<TokenVerifier>().Setup(x => x.Verify(It.IsAny<string>())).Returns((string value) =>
            {
                var token = new JwtSecurityTokenHandler().ReadJwtToken(value);
                return new TokenClaims
                {
                    Id = token.Id, Identity = token.Subject, RawUserName = "test-user",
                    MustChangePassword = token.Payload.ContainsKey(MultiFactorClaims.ChangePassword),
                    SamlClaim = token.Payload.TryGetValue(MultiFactorClaims.SamlSessionId, out var saml) ? (string)saml : null,
                    OidcClaim = token.Payload.TryGetValue(MultiFactorClaims.OidcSessionId, out var oidc) ? (string)oidc : null
                };
            });
            _story = _mocker.CreateInstance<AuthenticateSessionStory>();
        }

        private static string Token(bool mustChange, bool saml, string requestId = "request-id")
        {
            var payload = new JwtPayload
            {
                ["sub"] = "test-user", ["jti"] = requestId, [MultiFactorClaims.RawUserName] = "test-user",
                ["amr"] = "pwd", ["groups"] = new[] { "a", "b" },
                [saml ? MultiFactorClaims.SamlSessionId : MultiFactorClaims.OidcSessionId] = "sso-session"
            };
            if (mustChange) payload[MultiFactorClaims.ChangePassword] = "true";
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(), payload));
        }

        private void SetupIdp(string token, LoginCompletedAction action, bool success = true)
        {
            _mocker.GetMock<IMultifactorIdpApi>().Setup(x => x.LoginCompletedAsync(
                It.Is<LoginCompletedRequestDto>(r => r.AccessToken == token), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(new LoginCompletedResponseDto
                {
                    Success = success, Action = action, Identity = "test-user",
                    SamlSessionId = action == LoginCompletedAction.BypassSaml ? "sso-session" : null,
                    OidcSessionId = action == LoginCompletedAction.BypassOidc ? "sso-session" : null,
                    TokenExpirationDate = DateTime.UtcNow.AddMinutes(5)
                });
        }

        [Theory]
        [InlineData(LoginCompletedAction.Authenticated, "Index")]
        [InlineData(LoginCompletedAction.BypassSaml, "ByPassSamlSession")]
        [InlineData(LoginCompletedAction.BypassOidc, "ByPassOidcSession")]
        public async Task NormalCompletion_PreservesActionAndCookieWithoutRefresh(LoginCompletedAction action, string expected)
        {
            SetupIdp("ordinary-token", action);

            var result = Assert.IsType<RedirectToRouteResult>(await _story.Execute("ordinary-token"));

            Assert.Equal(expected, result.RouteValues["action"]);
            Assert.Equal("ordinary-token", LastTokenCookie().Value);
            _mocker.GetMock<MultiFactorApiClient>().Verify(x => x.RefreshAccessToken(
                It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public async Task MandatoryChange_ForwardsRequiredTokenAndPrioritizesFormOverSso(bool alreadyMarked, bool saml)
        {
            var original = Token(alreadyMarked, saml);
            var marked = Token(true, saml);
            _mocker.GetMock<MultiFactorApiClient>().Setup(x => x.RefreshAccessToken(original,
                It.Is<IDictionary<string, string>>(c => c[MultiFactorClaims.ChangePassword] == "true"
                    && c["amr"] == "pwd" && c["groups"] == "[\"a\",\"b\"]"))).Returns(marked);
            var expectedToken = alreadyMarked ? original : marked;
            SetupIdp(expectedToken, saml ? LoginCompletedAction.BypassSaml : LoginCompletedAction.BypassOidc);

            var result = Assert.IsType<RedirectToRouteResult>(await _story.ExecutePasswordChangeAsync(original));

            Assert.Equal("Change", result.RouteValues["action"]);
            Assert.Equal(expectedToken, LastTokenCookie().Value);
            _mocker.GetMock<MultiFactorApiClient>().Verify(x => x.RefreshAccessToken(
                It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), alreadyMarked ? Times.Never() : Times.Once());
            _mocker.GetMock<IMultifactorIdpApi>().Verify(x => x.LoginCompletedAsync(
                It.Is<LoginCompletedRequestDto>(r => r.AccessToken == expectedToken), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PasswordChangeCompletion_ContinuesOriginalSso(bool saml)
        {
            var refreshedToken = Token(false, saml);
            SetupIdp(refreshedToken, saml ? LoginCompletedAction.BypassSaml : LoginCompletedAction.BypassOidc);

            var result = Assert.IsType<RedirectToRouteResult>(await _story.ExecuteAfterPasswordChangeAsync(
                refreshedToken, saml ? "sso-session" : null, saml ? null : "sso-session"));

            Assert.Equal(saml ? "ByPassSamlSession" : "ByPassOidcSession", result.RouteValues["action"]);
            Assert.Equal("sso-session", result.RouteValues[saml ? "samlSession" : "oidcSession"]);
            Assert.Equal(refreshedToken, LastTokenCookie().Value);
            Assert.True(LastTokenCookie().Expires > DateTime.UtcNow);
            _mocker.GetMock<IMultifactorIdpApi>().Verify(x => x.LoginCompletedAsync(
                It.Is<LoginCompletedRequestDto>(r => r.AccessToken == refreshedToken), It.IsAny<Dictionary<string, string>>()), Times.Once);
            _mocker.GetMock<MultiFactorApiClient>().Verify(x => x.RefreshAccessToken(
                It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
        }

        [Theory]
        [InlineData("saml-session", null)]
        [InlineData(null, "oidc-session")]
        public async Task PasswordChangeCompletion_IdpErrorRestartsOriginalLogin(string saml, string oidc)
        {
            SetupIdp("refreshed-token", LoginCompletedAction.Error, false);

            var result = Assert.IsType<RedirectToRouteResult>(await _story.ExecuteAfterPasswordChangeAsync(
                "refreshed-token", saml, oidc));

            Assert.Equal("Login", result.RouteValues["action"]);
            Assert.Equal(saml, result.RouteValues["samlSessionId"]);
            Assert.Equal(oidc, result.RouteValues["oidcSessionId"]);
            Assert.DoesNotContain(Constants.COOKIE_NAME, HttpContext.Current.Response.Cookies.AllKeys);
            _mocker.GetMock<IMultifactorIdpApi>().Verify(x => x.LoginCompletedAsync(
                It.Is<LoginCompletedRequestDto>(r => r.AccessToken == "refreshed-token"), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Fact]
        public async Task PasswordChangeCompletion_WithoutSsoShowsDone()
        {
            SetupIdp("refreshed-token", LoginCompletedAction.Authenticated);

            var result = Assert.IsType<RedirectToRouteResult>(await _story.ExecuteAfterPasswordChangeAsync("refreshed-token"));

            Assert.Equal("Done", result.RouteValues["action"]);
            Assert.Equal("ExpiredPassword", result.RouteValues["controller"]);
            Assert.Equal("refreshed-token", LastTokenCookie().Value);
        }

        [Theory]
        [InlineData(LoginCompletedAction.Authenticated, true, "Index")]
        [InlineData(LoginCompletedAction.BypassSaml, true, "ByPassSamlSession")]
        [InlineData(LoginCompletedAction.BypassOidc, true, "ByPassOidcSession")]
        [InlineData(LoginCompletedAction.AccessDenied, false, "AccessDenied")]
        public async Task ValidPassword_AuthnPreservesIdpActionWithoutRefresh(LoginCompletedAction action, bool success, string expected)
        {
            _mocker.GetMock<ICredentialVerifier>().Setup(x => x.VerifyCredentialAsync("jdoe@test.local", "password"))
                .ReturnsAsync(CredentialVerificationResult.CreateBuilder(true).SetUsername("jdoe@test.local").Build());
            _mocker.Use(_story);
            SetupIdp("ordinary-token", action, success);

            var result = Assert.IsType<RedirectToRouteResult>(await _mocker.CreateInstance<AuthnStory>().ExecuteAsync(
                new IdentityModel { UserName = "jdoe@test.local", Password = "password", AccessToken = "ordinary-token" }));

            Assert.Equal(expected, result.RouteValues["action"]);
            _mocker.GetMock<MultiFactorApiClient>().Verify(x => x.RefreshAccessToken(
                It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
            _mocker.GetMock<IApplicationCache>().Verify(x => x.Set(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static HttpCookie LastTokenCookie()
        {
            var cookies = HttpContext.Current.Response.Cookies;
            return Enumerable.Range(0, cookies.Count).Select(cookies.Get).Last(cookie => cookie.Name == Constants.COOKIE_NAME);
        }

        public void Dispose() => _context.Dispose();
    }
}
