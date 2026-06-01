using System.Threading.Tasks;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification
{

    public interface ICredentialVerifier
    {
        Task<CredentialVerificationResult> VerifyCredentialAsync(string username, string password);
        Task VerifyCredentialOnlyAsync(string username, string password);
        Task<CredentialVerificationResult> VerifyMembership(string username);
    }
}