using System;

namespace MultiFactor.SelfService.Windows.Portal.Services.Caching
{
    public class ApplicationCacheConfig
    {
        public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(2);
        public TimeSpan SupportInfoExpiration { get; set; } = TimeSpan.FromMinutes(60);
        public TimeSpan SupportInfoEmptyExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }
}