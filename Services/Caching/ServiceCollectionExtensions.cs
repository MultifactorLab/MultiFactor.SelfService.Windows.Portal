using Microsoft.Extensions.DependencyInjection;

namespace MultiFactor.SelfService.Windows.Portal.Services.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApplicationCache(this IServiceCollection services)
        {
            var config = services.BuildServiceProvider().GetRequiredService<Configuration>();
            services.AddMemoryCache(x =>
            {
                // 5 Mb by default
                var pwdSessionCache = config.PwdChangingSessionCacheSize ?? 1024 * 1024 * 5;
                var supportInfoCache = Constants.Configuration.SupportInfoCache.SUPPORT_INFO_CACHE_SIZE;
                x.SizeLimit = pwdSessionCache + supportInfoCache;
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