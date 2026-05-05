using System;
using System.Collections.Generic;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Core.Metadata.GlobalValues;
using MultiFactor.SelfService.Windows.Portal.Extensions;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignIn.ClaimsSources
{
    public class ClaimValuesContext : IApplicationValuesContext
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationGlobalValuesProvider _globalValuesProvider;

        public ClaimValuesContext(SafeHttpContextAccessor httpContextAccessor, ApplicationGlobalValuesProvider globalValuesProvider)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _globalValuesProvider = globalValuesProvider ?? throw new ArgumentNullException(nameof(globalValuesProvider));
        }

        public IReadOnlyList<string> this[string key] => GetValues(key);

        private IReadOnlyList<string> GetValues(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            if (ApplicationGlobalValuesMetadata.HasKey(key))
            {
                return _globalValuesProvider.GetValues(ApplicationGlobalValuesMetadata.ParseKey(key));
            }

            return _httpContextAccessor.SafeGetLdapAttributes().GetValues(key);
        }
    }
}