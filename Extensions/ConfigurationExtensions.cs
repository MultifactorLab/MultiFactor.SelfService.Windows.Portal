using System;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MultiFactor.SelfService.Windows.Portal.Extensions
{
    public static class ConfigurationExtensions
    {
        public static JsonWebKeySet GetJsonWebKeySet(this IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var value = config.GetValue<string>(Constants.TOKEN_VALIDATION);
            return new JsonWebKeySet(value);
        }

        public static string GetEnvironment(this IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.GetValue<string>(Constants.ENVIRONMENT_KEY);
        }
    }
}