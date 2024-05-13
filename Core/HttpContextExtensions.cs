using Microsoft.Extensions.DependencyInjection;
using System;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public static class HttpContextExtensions
    {
        public static IServiceProvider GetRequestServices(this HttpContextBase context)
        {
            var scope = context.Items[typeof(IServiceScope)] as IServiceScope;
            return scope?.ServiceProvider ?? throw new Exception("Service provider not configured");
        }
        
        public static IServiceProvider GetRequestServices(this HttpContext context)
        {
            var scope = context.Items[typeof(IServiceScope)] as IServiceScope;
            return scope?.ServiceProvider ?? throw new Exception("Service provider not configured");
        }

        public static string BuildCallbackUrl(this HttpContextBase context, string path)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (path is null) throw new ArgumentNullException(nameof(path));
            
            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(context.Request.Url.ToString());
            var noLastSegment = string.Format("{0}://{1}", currentUri.Scheme, currentUri.Authority);

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing
            return $"{noLastSegment.Trim("/".ToCharArray())}/{path}";
        }
    }
}