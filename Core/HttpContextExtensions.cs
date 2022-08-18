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
    }
}