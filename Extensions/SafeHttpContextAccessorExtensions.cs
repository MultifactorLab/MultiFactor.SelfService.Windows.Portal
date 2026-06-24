using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Core.LdapAttributesCaching;
using MultiFactor.SelfService.Windows.Portal.Models;

namespace MultiFactor.SelfService.Windows.Portal.Extensions
{
    public static class SafeHttpContextAccessorExtensions
    {
        public static SingleSignOnDto SafeGetSsoClaims(this SafeHttpContextAccessor accessor)
        {
            return accessor.HttpContext.Items[Constants.SsoClaims] as SingleSignOnDto
                ?? new SingleSignOnDto();
        }

        public static CredentialVerificationResult SafeGetCredVerificationResult(this SafeHttpContextAccessor accessor)
        {
            return accessor.HttpContext.Items[Constants.CredentialVerificationResult] as CredentialVerificationResult
                ?? CredentialVerificationResult.CreateBuilder(false).Build();
        }

        public static void SafeSetCredVerificationResult(this SafeHttpContextAccessor accessor, CredentialVerificationResult result)
        {
            accessor.HttpContext.Items[Constants.CredentialVerificationResult] = result;
        }

        public static ILdapAttributesCache SafeGetLdapAttributes(this SafeHttpContextAccessor accessor)
        {
            return accessor.HttpContext.Items[Constants.LoadedLdapAttributes] as ILdapAttributesCache
                ?? new LdapAttributesCache();
        }
    }
}
