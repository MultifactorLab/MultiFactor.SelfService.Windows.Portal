using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using MultiFactor.SelfService.Windows.Portal.Models;

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

        public void SetIdentity(string key, IdentityModel value)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_config.AbsoluteExpiration)
                .SetSize(GetDataSize(value));
            _cache.Set(key, value, options);
        }
        
        public CachedItem<IdentityModel> GetIdentity(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return CachedItem<IdentityModel>.Empty;
            return _cache.TryGetValue(key, out IdentityModel value) 
                ? new CachedItem<IdentityModel>(value) 
                : CachedItem<IdentityModel>.Empty;
        }
        
        public CachedItem<string> Get(string key)
        {
            return _cache.TryGetValue(key, out string value) 
                ? new CachedItem<string>(value) 
                : CachedItem<string>.Empty;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        private static long GetDataSize(string data)
        {
            return CalculateStringSize(data);
        }
        
        private static long GetDataSize(IdentityModel data)
        {
            return CalculateStringSize(data.AccessToken) +
                   CalculateStringSize(data.UserName);
        }

        public void SetSupportInfo(string key, SupportViewModel value)
        {
            var expiration = value.IsEmpty() 
                ? _config.SupportInfoEmptyExpiration 
                : _config.SupportInfoExpiration;
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration)
                .SetSize(GetDataSize(value));
            _cache.Set(key, value, options);
        }

        public CachedItem<SupportViewModel> GetSupportInfo(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return CachedItem<SupportViewModel>.Empty;
            return _cache.TryGetValue(key, out SupportViewModel value) 
                ? new CachedItem<SupportViewModel>(value) 
                : CachedItem<SupportViewModel>.Empty;
        }
        
        private static long GetDataSize(SupportViewModel data)
        {
            return CalculateStringSize(data.AdminEmail) + 
                   CalculateStringSize(data.AdminName) + 
                   CalculateStringSize(data.AdminPhone);
        }
        
        private static long CalculateStringSize(string s)
        {
            var headerSize = IntPtr.Size == 4 ? 14 : 22; // 32-bit ? 14 : 22
            var size = headerSize + 2L * s.Length;
            var alignment = IntPtr.Size == 4 ? 4 : 8; // 32-bit ? 4 : 8
            return (long)Math.Ceiling(size / (double)alignment) * alignment;
        }
    }
}