using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using MultiFactor.SelfService.Windows.Portal.Core;

namespace MultiFactor.SelfService.Windows.Portal.Extensions
{
    public static class PortalSettingsAccess
    {
        public static TProperty GetPortalSettingsValue<TProperty>(this IConfiguration config,
            Expression<Func<Configuration, TProperty>> propertySelector)
        {
            if (propertySelector == null)
            {
                throw new ArgumentNullException(nameof(propertySelector));
            }

            var key = ClassPropertyAccessor.GetPropertyPath(propertySelector, ":");
            return GetConfigValue<TProperty>(config, $"PortalSettings:{key}");
        }

        public static TProperty GetConfigValue<TProperty>(this IConfiguration config, string path)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return config.GetValue<TProperty>(path);
        }
    }
}