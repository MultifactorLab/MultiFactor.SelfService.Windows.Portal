using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.Description
{
    public class AdditionalClaimDescriptorsProvider
    {
        private readonly Configuration _portalSettings;

        public AdditionalClaimDescriptorsProvider(Configuration portalSettings)
        {
            _portalSettings = portalSettings ?? throw new ArgumentNullException(nameof(portalSettings));
        }

        public IReadOnlyList<AdditionalClaimDescriptor> GetDescriptors()
        {
            return Enumerable
                .Empty<AdditionalClaimDescriptor>()
                .ToList()
                .AsReadOnly();
        }
    }
}
