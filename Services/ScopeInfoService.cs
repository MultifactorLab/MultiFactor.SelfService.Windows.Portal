using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using Serilog;
using System;
using MultiFactor.SelfService.Windows.Portal.Services.API.DTO;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class ScopeInfoService
    {
        private readonly MultiFactorSelfServiceApiClient _apiClient;
        private readonly ApplicationCache _cache;
        private readonly ILogger _logger;
        
        public ScopeInfoService(
            MultiFactorSelfServiceApiClient apiClient, 
            ApplicationCache cache, 
            ILogger logger)
        {
            _apiClient = apiClient;
            _cache = cache;
            _logger = logger;
        }
        
        public SupportViewModel GetSupportInfo()
        {
            var cachedInfo = _cache.GetSupportInfo(Constants.Configuration.SupportInfoCache.SUPPORT_INFO_CACHE_KEY);
            if (!cachedInfo.IsEmpty)
            {
                return cachedInfo.Value;
            }
            
            return LoadAndCacheScopeInfo();
        }
        
        private SupportViewModel LoadAndCacheScopeInfo()
        {
            try
            {
                var apiResponse = _apiClient.GetScopeSupportInfo();
                var supportInfo = ScopeSupportInfoDto.ToModel(apiResponse.Model);
                _cache.SetSupportInfo(Constants.Configuration.SupportInfoCache.SUPPORT_INFO_CACHE_KEY, supportInfo);
                return supportInfo;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load scope info: {Message:l}", ex.Message);
                var emptyModel = SupportViewModel.EmptyModel();
                _cache.SetSupportInfo(Constants.Configuration.SupportInfoCache.SUPPORT_INFO_CACHE_KEY, emptyModel);
                return emptyModel;
            }
        }
    }
}
