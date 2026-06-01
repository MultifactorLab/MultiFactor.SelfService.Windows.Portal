using System;

namespace MultiFactor.SelfService.Windows.Portal.Authentication
{
    public class TokenClaims
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public string RawUserName { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime ValidTo { get; set; }
        public bool MustResetPassword { get; set; }
        public string SamlClaim { get; set; }
        public string OidcClaim { get; set; }
        public bool MustUnlockUser { get; set; }

        public TokenClaims() { }

        public TokenClaims(
            string id,
            string identity,
            string rawUserName,
            bool mustChangePassword,
            DateTime validTo,
            bool mustResetPassword,
            string samlClaim,
            string oidcClaim,
            bool mustUnlockUser = false)
        {
            Id = id;
            Identity = identity;
            RawUserName = rawUserName;
            MustChangePassword = mustChangePassword;
            ValidTo = validTo;
            MustResetPassword = mustResetPassword;
            SamlClaim = samlClaim;
            OidcClaim = oidcClaim;
            MustUnlockUser = mustUnlockUser;
        }
    }
}
