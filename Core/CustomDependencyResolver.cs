using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public class CustomDependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _provider;

        public CustomDependencyResolver(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public object GetService(Type serviceType)
        {
            return _provider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _provider.GetServices(serviceType);
        }
    }
}