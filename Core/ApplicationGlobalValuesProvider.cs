using System.Collections.Generic;
using System;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Core.Metadata.GlobalValues;
using MultiFactor.SelfService.Windows.Portal.Extensions;

namespace MultiFactor.SelfService.Windows.Portal.Core
{
    public class ApplicationGlobalValuesProvider
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;

        public ApplicationGlobalValuesProvider(SafeHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public IReadOnlyList<string> GetValues(ApplicationGlobalValue key)
        {
            switch (key)
            {
                case ApplicationGlobalValue.UserName:
                    return new[] { _httpContextAccessor.SafeGetCredVerificationResult().Username ?? string.Empty };

                case ApplicationGlobalValue.UserGroup:
                    return _httpContextAccessor.SafeGetLdapAttributes().GetValues("memberOf");

                default:
                    return Array.Empty<string>();
            }
        }
    }
}
