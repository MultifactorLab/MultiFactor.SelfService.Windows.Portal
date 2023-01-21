using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using Serilog;
using System;
using System.Web;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class AuthService
    {
        private readonly TokenValidationService _tokenValidationService;
        private readonly ILogger _logger;

        public AuthService(TokenValidationService tokenValidationService, ILogger logger)
        {
            _tokenValidationService = tokenValidationService ?? throw new ArgumentNullException(nameof(tokenValidationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void SignIn(string accessToken)
        {
            if (!_tokenValidationService.VerifyToken(accessToken, out var token))
            {
                throw new UnauthorizedException();
            }

            _logger.Information("Second factor for user '{user:l}' verified successfully", token.Identity);

            //save token to cookie
            //secure flag managed by web.config settings
            var cookie = new HttpCookie(Constants.COOKIE_NAME)
            {
                Value = accessToken,
                Expires = token.ValidTo
            };

            //remove mfa cookie
            if (HttpContext.Current.Response.Cookies[Constants.COOKIE_NAME] != null)
            {
                HttpContext.Current.Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.Now.AddDays(-1);
            }
            HttpContext.Current.Response.Cookies.Add(cookie);
            FormsAuthentication.SetAuthCookie(token.Identity, false);
        }
    }
}