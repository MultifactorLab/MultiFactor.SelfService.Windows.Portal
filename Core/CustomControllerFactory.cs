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
            if (requestContext == null)
            {
                throw new ArgumentNullException("requestContext");
            }

            if (controllerType == null)
            {
                throw new HttpException(404, $"The controller for path '{requestContext.HttpContext.Request.Path}' was not found or does not implement IController");
            }

            if (!typeof(IController).IsAssignableFrom(controllerType))
            {
                throw new ArgumentException($"Invalid controller type: {controllerType} is not subtype of 'IController'");
            }

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