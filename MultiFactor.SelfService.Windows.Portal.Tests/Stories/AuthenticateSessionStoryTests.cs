using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Stories
{
    public class AuthenticateSessionStoryTests
    {
        [Fact]
        public async Task PasswordChangeCompletion_WhenIdpDenies_DoesNotContinueSso()
        {
            var previous = HttpContext.Current;
            try
            {
                HttpContext.Current = new HttpContext(new HttpRequest(string.Empty, "https://ssp.example/", string.Empty),
                    new HttpResponse(TextWriter.Null));
                var idp = new Mock<IMultifactorIdpApi>();
                idp.Setup(x => x.LoginCompletedAsync(It.Is<LoginCompletedRequestDto>(r => r.AccessToken == "refreshed-token"),
                    It.IsAny<Dictionary<string, string>>())).ReturnsAsync(new LoginCompletedResponseDto
                    {
                        Success = false, Action = LoginCompletedAction.AccessDenied
                    });
                var story = new AuthenticateSessionStory(idp.Object, new SafeHttpContextAccessor(), Mock.Of<ILogger>(), new Configuration(), null, null);

                var result = Assert.IsType<RedirectToRouteResult>(await story.ExecuteAfterPasswordChangeAsync("refreshed-token"));

                Assert.Equal("AccessDenied", result.RouteValues["action"]);
                idp.Verify(x => x.LoginCompletedAsync(It.Is<LoginCompletedRequestDto>(r => r.AccessToken == "refreshed-token"),
                    It.IsAny<Dictionary<string, string>>()), Times.Once);
                Assert.Null(HttpContext.Current.Response.Cookies[Constants.COOKIE_NAME].Value);
            }
            finally
            {
                HttpContext.Current = previous;
            }
        }

        [Fact]
        public async Task PasswordManagementDisabled_DeniesWithoutSettingCookie()
        {
            var previous = HttpContext.Current;
            try
            {
                HttpContext.Current = new HttpContext(new HttpRequest(string.Empty, "https://ssp.example/", string.Empty),
                    new HttpResponse(TextWriter.Null));
                var idp = new Mock<IMultifactorIdpApi>();
                idp.Setup(x => x.LoginCompletedAsync(It.IsAny<LoginCompletedRequestDto>(), It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync(new LoginCompletedResponseDto
                    {
                        Success = true, Action = LoginCompletedAction.ChangePassword,
                        TokenExpirationDate = System.DateTime.UtcNow.AddMinutes(5)
                    });
                var story = new AuthenticateSessionStory(idp.Object, new SafeHttpContextAccessor(), Mock.Of<ILogger>(), new Configuration(), null, null);

                var result = Assert.IsType<RedirectToRouteResult>(await story.Execute("access-token"));

                Assert.Equal("AccessDenied", result.RouteValues["action"]);
                Assert.DoesNotContain(Constants.COOKIE_NAME, HttpContext.Current.Response.Cookies.AllKeys);
            }
            finally
            {
                HttpContext.Current = previous;
            }
        }
    }
}
