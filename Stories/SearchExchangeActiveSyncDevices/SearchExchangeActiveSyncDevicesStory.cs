using System.Collections.Generic;
using System.Threading.Tasks;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SearchExchangeActiveSyncDevices
{
    public class SearchExchangeActiveSyncDevicesStory
    {
        private readonly ActiveDirectoryService _activeDirectoryService;

        public SearchExchangeActiveSyncDevicesStory(ActiveDirectoryService activeDirectoryService)
        {
            _activeDirectoryService = activeDirectoryService;
        }

        public async Task<IList<ExchangeActiveSyncDevice>> ExecuteAsync(string identity)
        {
            return _activeDirectoryService.SearchExchangeActiveSyncDevices(identity);
        }
    }
}
