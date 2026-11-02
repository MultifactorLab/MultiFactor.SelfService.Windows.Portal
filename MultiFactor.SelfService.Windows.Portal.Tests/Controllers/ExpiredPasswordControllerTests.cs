using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using MultiFactor.SelfService.Windows.Portal.Authentication;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;
using MultiFactor.SelfService.Windows.Portal.Controllers;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;
using MultiFactor.SelfService.Windows.Portal.Tests.Fixtures;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Controllers
{
    public class ExpiredPasswordControllerTests : IDisposable
    {
        private const string Username = "jdoe";
        private const string AccessToken = "expired-token";
        private readonly PortalHttpContext _context = new PortalHttpContext();
        private readonly AutoMocker _mocker = new AutoMocker();
        private readonly MemoryCache _memory = new MemoryCache(new MemoryCacheOptions());
        private readonly ApplicationCache _cache;
        private readonly ExpiredPasswordController _controller;
        private readonly Configuration _settings;

        public ExpiredPasswordControllerTests()
        {
            _settings = new Configuration { PasswordRequirements = new PasswordRequirements() };
            _mocker.Use(_settings);
            HttpContext.Current.User = new GenericPrincipal(new GenericIdentity("jdoe@test.local", "test"), new string[0]);
            HttpContext.Current.Request.Cookies.Set(new HttpCookie(Constants.COOKIE_NAME, AccessToken));
            _cache = new ApplicationCache(_memory, new OptionsWrapper<ApplicationCacheConfig>(new ApplicationCacheConfig()));
            _cache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(Username), Username);
            _cache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username), "encrypted-password");
            _mocker.Use(_cache);
            _mocker.Use(new JwtTokenProvider());
            _mocker.Use(new PasswordPolicyService(_settings));
            _mocker.GetMock<TokenVerifier>().Setup(x => x.Verify(AccessToken)).Returns(new TokenClaims
            {
                Identity = "jdoe@test.local", RawUserName = Username, SamlClaim = "saml-session", MustChangePassword = true
            });
            _mocker.GetMock<DataProtectionService>().Setup(x => x.Unprotect("encrypted-password")).Returns("old-password");
            _controller = _mocker.CreateInstance<ExpiredPasswordController>();
            _controller.ControllerContext = new ControllerContext(new HttpContextWrapper(HttpContext.Current), new RouteData(), _controller);
        }

        private Task<ActionResult> ChangeAsync() => _controller.Change(new ChangeExpiredPasswordModel
        {
            NewPassword = "new-password", NewPasswordAgain = "new-password"
        });

        [Fact]
        public async Task LdapFailure_KeepsFormAndSessionWithoutCallingApi()
        {
            string reason = "password rejected";
            _mocker.GetMock<ActiveDirectoryService>().Setup(x => x.ChangeExpiredPassword(Username,
                "old-password", "new-password", out reason)).Returns(false);

            Assert.IsType<ViewResult>(await ChangeAsync());

            Assert.Contains(_controller.ModelState.Values, state =>
                state.Errors.Any(error => error.ErrorMessage == reason));
            Assert.False(_cache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username)).IsEmpty);
            _mocker.GetMock<MultiFactorApiClient>().Verify(x => x.RefreshAccessToken(
                It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
            AssertNoLoginCompletion();
        }

        [Fact]
        public async Task PasswordPolicyFailure_DoesNotChangePasswordOrClearSession()
        {
            _settings.PasswordRequirements.MinLength = new PasswordRequirementElement
            {
                Enabled = true, Value = "100", Condition = Constants.Configuration.PasswordRequirements.MIN_LENGTH
            };

            Assert.IsType<ViewResult>(await ChangeAsync());

            Assert.False(_controller.ModelState.IsValid);
            Assert.False(_cache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username)).IsEmpty);
            AssertNoPasswordChange();
        }

        [Fact]
        public async Task ExpiredChangeSession_DoesNotChangePasswordOrCallApi()
        {
            _cache.Remove(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username));

            var error = await Assert.ThrowsAnyAsync<Exception>(() => ChangeAsync());

            Assert.Equal("PasswordChangingSessionExpired", error.GetType().Name);
            AssertNoPasswordChange();
        }

        [Fact]
        public async Task DirectChange_RefreshFailureDoesNotSignInOrCompleteIdp()
        {
            _mocker.GetMock<TokenVerifier>().Setup(x => x.Verify(AccessToken)).Returns(new TokenClaims
            {
                Identity = "jdoe@test.local", RawUserName = Username, MustChangePassword = true
            });
            string reason = null;
            _mocker.GetMock<ActiveDirectoryService>().Setup(x => x.ChangeExpiredPassword(Username,
                "old-password", "new-password", out reason)).Returns(true);
            var failure = new InvalidOperationException("refresh unavailable");
            _mocker.GetMock<MultiFactorApiClient>().Setup(x => x.RefreshAccessToken(AccessToken,
                It.Is<IDictionary<string, string>>(claims => claims.Count == 1
                    && claims[MultiFactorClaims.RawUserName] == "jdoe@test.local"))).Throws(failure);

            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => ChangeAsync()));

            Assert.False(_cache.Get(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(Username)).IsEmpty);
            AssertNoLoginCompletion();
        }

        private void AssertNoPasswordChange()
        {
            _mocker.GetMock<ActiveDirectoryService>().Verify(x => x.ChangeExpiredPassword(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), out It.Ref<string>.IsAny), Times.Never);
            _mocker.GetMock<MultiFactorApiClient>().Verify(x => x.RefreshAccessToken(
                It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()), Times.Never);
            AssertNoLoginCompletion();
        }

        private void AssertNoLoginCompletion()
        {
            _mocker.GetMock<AuthService>().Verify(x => x.SignIn(It.IsAny<string>()), Times.Never);
            _mocker.GetMock<IMultifactorIdpApi>().Verify(x => x.LoginCompletedAsync(
                It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        }

        public void Dispose()
        {
            _context.Dispose();
            _memory.Dispose();
        }
    }
}
