using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Settings;
using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.ViewModels
{
    public class ShowcaseViewModel
    {
        public UserProfileDto Profile { get; set; }

        public IReadOnlyCollection<ShowcaseLink> ShowcaseLinks { get; set; }
    }
}
