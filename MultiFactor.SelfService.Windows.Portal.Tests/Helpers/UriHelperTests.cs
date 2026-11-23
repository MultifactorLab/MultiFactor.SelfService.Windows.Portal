using MultiFactor.SelfService.Windows.Portal.Helpers;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Helpers
{
    public class UriHelperTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("JaVaScRiPt:alert(1)", false)]
        [InlineData("data:text/html;base64,AAAA", false)]
        [InlineData("vbscript:msgbox(1)", false)]
        [InlineData("http://company.com/cb", false)]
        [InlineData("//company.com/cb", false)] 
        [InlineData("/callback", false)] // относительный
        [InlineData("https:callback", false)] 
        [InlineData("https://u:p@company.com/cb", false)] 
        [InlineData(" https://company.com/cb ", true)] 
        [InlineData("HTTPS://COMPANY.COM/cb", true)] 
        [InlineData("https://company.com/cb?state=1#f", true)]
        public void TryReadHttpsUrl_ReadsExpected(string url, bool expected)
        {
            Assert.Equal(UrlHelper.TryReadHttpsUrl(url, out _), expected);
        }
    }
}