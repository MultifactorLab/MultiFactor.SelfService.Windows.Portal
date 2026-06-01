using System;
using System.Web;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;

namespace MultiFactor.SelfService.Windows.Portal.Core.Http
{
    public class HttpClientTokenProvider
    {
        private readonly SafeHttpContextAccessor _contextAccessor;

        public HttpClientTokenProvider(SafeHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public string GetToken()
        {
            var cookie = _contextAccessor.HttpContext.Request.Cookies[Constants.COOKIE_NAME];

            if (cookie == null)
                throw new UnauthorizedException("HttpClient token not found");

            return cookie.Value;
        }
    }

    public class SafeHttpContextAccessor
    {
        public HttpContext HttpContext => HttpContext.Current ?? throw new HttpContextNotDefinedException("HttpContext can't be null here");
    }

    internal class HttpContextNotDefinedException : Exception
    {
        public HttpContextNotDefinedException() { }
        public HttpContextNotDefinedException(string message) : base(message) { }
        public HttpContextNotDefinedException(string message, Exception inner) : base(message, inner) { }
    }
}