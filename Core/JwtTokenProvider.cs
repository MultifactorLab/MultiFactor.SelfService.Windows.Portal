using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public class JwtTokenProvider
    {
        public string GetToken()
        {
            var tokenCookie = HttpContext.Current.Request.Cookies[Constants.COOKIE_NAME];
            return tokenCookie?.Value ?? throw new UnauthorizedException();
        }
    }
}