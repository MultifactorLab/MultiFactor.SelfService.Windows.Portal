using System.Threading.Tasks;
using System;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Windows.Portal.Stories.LoadProfile
{
    public class LoadProfileStory
    {
        private readonly IMultiFactorApi _api;

        public LoadProfileStory(IMultiFactorApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public Task<UserProfileDto> ExecuteAsync() => _api.GetUserProfileAsync();
    }
}
