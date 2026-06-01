using System.Threading.Tasks;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification
{
    public class CredentialVerifierAdapter : ICredentialVerifier
    {
        private ActiveDirectoryService _activeDirectoryService;

        public CredentialVerifierAdapter(ActiveDirectoryService activeDirectoryService)
        {
            _activeDirectoryService = activeDirectoryService;
        }

        public async Task<CredentialVerificationResult> VerifyCredentialAsync(string username, string password)
        {
            var adResult = _activeDirectoryService.VerifyCredentialAndMembership(username, password);
            var result = BuildVerificationResult(adResult, username);
            return result;
        }

        public async Task VerifyCredentialOnlyAsync(string username, string password)
        {
            var adResult = _activeDirectoryService.VerifyCredentialAndMembership(username, password);
        }

        public async Task<CredentialVerificationResult> VerifyMembership(string username)
        {
            var adResult = _activeDirectoryService.VerifyMembership(LdapIdentity.ParseUser(username.Trim()));
            var result = BuildVerificationResult(adResult, username);
            return result;
        }

        private static CredentialVerificationResult BuildVerificationResult(ActiveDirectoryCredentialValidationResult adResult, string username)
        {
            var builder = CredentialVerificationResult.CreateBuilder(adResult.IsAuthenticated);
            builder.SetPhone(adResult.Phone);
            builder.SetEmail(adResult.Email);
            builder.SetDisplayName(adResult.DisplayName);
            builder.SetReason(adResult.Reason);
            builder.SetCustomIdentity(adResult.GetIdentity(username));
            builder.SetPasswordExpirationDate(adResult.PasswordExpirationDate ?? System.DateTime.MaxValue);
            builder.SetUserMustChangePassword(adResult.UserMustChangePassword);
            builder.SetUsername(username);
            builder.SetUserPrincipalName(adResult.Upn);
            var result = builder.Build();
            return result;
        }
    }
}