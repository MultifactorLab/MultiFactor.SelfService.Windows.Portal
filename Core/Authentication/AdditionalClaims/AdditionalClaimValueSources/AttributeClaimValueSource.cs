using System.Collections.Generic;
using System;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources
{
    public class AttributeClaimValueSource : IClaimValueSource
    {
        public string Attribute { get; }

        public AttributeClaimValueSource(string attribute)
        {
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        }

        public IReadOnlyList<string> GetValues(IApplicationValuesContext context)
        {
            return context[Attribute];
        }
    }
}
