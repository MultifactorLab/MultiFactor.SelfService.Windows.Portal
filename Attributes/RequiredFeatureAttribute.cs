using System;
using System.ComponentModel;
using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;

namespace MultiFactor.SelfService.Windows.Portal.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequiredFeatureAttribute : ActionFilterAttribute
    {
        private readonly ApplicationFeature _requiredFeatureFlags;

        public RequiredFeatureAttribute(ApplicationFeature requiredFeatureFlags)
        {
            _requiredFeatureFlags = requiredFeatureFlags;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var scope = (IServiceScope)filterContext.HttpContext.Items[typeof(IServiceScope)];
            var configuration = scope.ServiceProvider.GetRequiredService<Configuration>();

            if (_requiredFeatureFlags.HasFlag(ApplicationFeature.PasswordManagement) && !configuration.EnablePasswordManagement)
            {
                throw new FeatureNotEnabledException(ApplicationFeature.PasswordManagement.GetEnumDescription());
            }
            
            if (_requiredFeatureFlags.HasFlag(ApplicationFeature.ExchangeActiveSyncDevicesManagement) && !configuration.EnableExchangeActiveSyncDevicesManagement)
            {
                throw new FeatureNotEnabledException(ApplicationFeature.ExchangeActiveSyncDevicesManagement.GetEnumDescription());
            }
        }
    }

    [Flags]
    public enum ApplicationFeature
    {
        None = 0,

        [Description("Password Management")]
        PasswordManagement = 1,

        [Description("Exchange Active Sync Devices Management")]
        ExchangeActiveSyncDevicesManagement = 2
    }
}