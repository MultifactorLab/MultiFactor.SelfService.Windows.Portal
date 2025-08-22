using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using Serilog;
using System;
using MultiFactor.SelfService.Windows.Portal.Services.API.DTO;
using Microsoft.Extensions.DependencyInjection;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class ScopeInfoService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private SupportInfo _supportInfo;
        private bool _isInitialized;
        private readonly object _lock = new object();
        
        public ScopeInfoService(IServiceScopeFactory serviceScopeFactory, ILogger logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        
        public SupportInfo GetAdminInfo()
        {
            if (!_isInitialized)
            {
                lock (_lock)
                {
                    if (_isInitialized) return _supportInfo;
                    LoadScopeInfo();
                }
            }
            return _supportInfo;
        }
        
        private void LoadScopeInfo()
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var apiClient = scope.ServiceProvider.GetRequiredService<MultiFactorSelfServiceApiClient>();
                    var apiResponse = apiClient.GetScopeSupportInfo();
                    _supportInfo = ScopeSupportInfoDto.ToModel(apiResponse.Model);
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load scope info: {Message}", ex.Message);
            }
        }
    }
}
