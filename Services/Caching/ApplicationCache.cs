using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;

namespace MultiFactor.SelfService.Windows.Portal.Services.Caching
{
    public class ApplicationCache
    {
        private readonly IMemoryCache _cache;
        private readonly ApplicationCacheConfig _config;

        public ApplicationCache(IMemoryCache cache, IOptions<ApplicationCacheConfig> options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public void Set(string key, string value)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_config.AbsoluteExpiration)
                .SetSize(GetDataSize(value));
            _cache.Set(key, value, options);
        }

        public CachedItem<string> Get(string key)
        {
            if (_cache.TryGetValue(key, out string value)) return new CachedItem<string>(value);
            return CachedItem<string>.Empty;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        private static long GetDataSize(string data)
        {
            if (string.IsNullOrEmpty(data)) return 18;
            return 18 + data.Length * 2;
        }
    }
}