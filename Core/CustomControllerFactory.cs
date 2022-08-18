using Microsoft.Extensions.DependencyInjection;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public class CustomControllerFactory : DefaultControllerFactory
    {
        private readonly ServiceProvider _provider;

        public CustomControllerFactory(ServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            IServiceScope scope = _provider.CreateScope();
            HttpContext.Current.Items[typeof(IServiceScope)] = scope;

            return (IController)scope.ServiceProvider.GetRequiredService(controllerType);
        }

        public override void ReleaseController(IController controller)
        {
            base.ReleaseController(controller);

            var scope = HttpContext.Current.Items[typeof(IServiceScope)] as IServiceScope;
            scope?.Dispose();
        }
    }
}