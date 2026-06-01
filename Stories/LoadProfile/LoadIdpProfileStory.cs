using System.Threading.Tasks;
using System;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi;

namespace MultiFactor.SelfService.Windows.Portal.Stories.LoadProfile
{
    public class LoadIdpProfileStory
    {
        private readonly IMultifactorIdpApi _api;

        public LoadIdpProfileStory(IMultifactorIdpApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public Task<UserProfileDto> ExecuteAsync() => _api.GetUserProfileAsync();
    }
}
