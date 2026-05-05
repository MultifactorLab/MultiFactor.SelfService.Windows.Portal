using System.Collections.Generic;
using System;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Settings;
using MultiFactor.SelfService.Windows.Portal.Options;
using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Stories.LoadProfileStory
{
    public class FilterShowcaseLinksStory
    {
        private readonly IShowcaseSettingsOptions _showcaseSettings;

        public FilterShowcaseLinksStory(IShowcaseSettingsOptions showcaseSettings)
        {
            _showcaseSettings = showcaseSettings;
        }

        public IReadOnlyCollection<ShowcaseLink> Execute(UserProfilePolicyDto policy)
        {
            var allLinks = _showcaseSettings.CurrentValue?.Links ?? Array.Empty<ShowcaseLink>();

            if (policy?.AllResourcesPermitted == true)
            {
                return allLinks;
            }

            var permittedResources = policy?.PermittedResources != null
                ? new HashSet<string>(policy.PermittedResources)
                : new HashSet<string>();

            var filtered = allLinks
                .Where(x => !string.IsNullOrWhiteSpace(x.ResourceId) && permittedResources.Contains(x.ResourceId))
                .ToArray();

            return filtered;
        }
    }
}
