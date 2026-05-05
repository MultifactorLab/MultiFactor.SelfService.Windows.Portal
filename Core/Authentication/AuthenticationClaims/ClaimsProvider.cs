using System.Collections.Generic;
using System;

namespace MultiFactor.SelfService.Windows.Portal.Core.Authentication.AuthenticationClaims
{
    public class ClaimsProvider
    {
        private readonly IEnumerable<IClaimsSource> _claimsSources;

        public ClaimsProvider(IEnumerable<IClaimsSource> claimsSources)
        {
            _claimsSources = claimsSources ?? throw new ArgumentNullException(nameof(claimsSources));
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var dict = new Dictionary<string, string>();

            foreach (var source in _claimsSources)
            {
                foreach (var claim in source.GetClaims())
                {
                    dict[claim.Key] = claim.Value;
                }
            }

            return dict;
        }
    }
}
