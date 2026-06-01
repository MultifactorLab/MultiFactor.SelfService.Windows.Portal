using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services.Caching;

namespace MultiFactor.SelfService.Windows.Portal.Core.Caching
{
    public interface IApplicationCache
    {
        void Set(string key, string value);
        CachedItem<string> Get(string key);
        void SetIdentity(string key, IdentityModel value);
        CachedItem<IdentityModel> GetIdentity(string key);
        void Remove(string key);
        void SetSupportInfo(string key, SupportViewModel value);
        CachedItem<SupportViewModel> GetSupportInfo(string key);
        void SetPreauthenticationAuthn(string key, bool value);
        CachedItem<bool> GetPreauthenticationAuthn(string key);
    }
}
