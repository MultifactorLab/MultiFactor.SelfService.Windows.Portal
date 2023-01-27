using Microsoft.Extensions.DependencyInjection;

namespace MultiFactor.SelfService.Windows.Portal.Services.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static void AddPasswordChangingSessionCache(this IServiceCollection services)
        {
            var config = services.BuildServiceProvider().GetRequiredService<Configuration>();
            services.AddMemoryCache(x =>
            {
                // 5 Mb by default
                x.SizeLimit = config.PwdChangingSessionCacheSize ?? 1024 * 1024 * 5;
            });

            services.Configure<ApplicationCacheConfig>(x =>
            {
                if (config.PwdChangingSessionLifetime != null)
                {
                    x.AbsoluteExpiration = config.PwdChangingSessionLifetime.Value;
                }
            });        
            services.AddSingleton<ApplicationCache>();
        }
    }

}