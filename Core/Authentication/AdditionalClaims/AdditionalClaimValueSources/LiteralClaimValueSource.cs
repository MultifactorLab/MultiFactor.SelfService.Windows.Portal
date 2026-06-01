using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources
{
    public class LiteralClaimValueSource : IClaimValueSource
    {
        public string Value { get; }

        public LiteralClaimValueSource(string value)
        {
            Value = value;
        }

        public IReadOnlyList<string> GetValues(IApplicationValuesContext context)
        {
            return new[] { Value };
        }
    }
}
