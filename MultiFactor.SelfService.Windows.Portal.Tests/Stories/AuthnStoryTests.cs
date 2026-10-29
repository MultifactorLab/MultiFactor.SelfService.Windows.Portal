using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Moq;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Windows.Portal.Stories.SignIn;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Stories
{
    public class AuthnStoryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task WithoutMfaToken_ShowsLoginWithoutAuthenticating(string token)
        {
            var verifier = new Mock<ICredentialVerifier>();
            var idp = new Mock<IMultifactorIdpApi>();
            var authenticate = new AuthenticateSessionStory(idp.Object, null, Mock.Of<ILogger>(), new Configuration(), null, null);
            var story = new AuthnStory(verifier.Object, null, null, new Configuration(),
                Mock.Of<IApplicationCache>(), Mock.Of<ILogger>(), authenticate);

            var result = Assert.IsType<ViewResult>(await story.ExecuteAsync(new IdentityModel
            {
                UserName = "jdoe@test.local", Password = "password", AccessToken = token
            }));

            Assert.Equal("Login", result.ViewName);
            var model = Assert.IsType<LoginModel>(result.Model);
            Assert.Equal("jdoe@test.local", model.UserName);
            Assert.Null(model.Password);
            verifier.Verify(x => x.VerifyCredentialAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            idp.Verify(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(),
                It.IsAny<Dictionary<string, string>>()), Times.Never);
        }
    }
}
