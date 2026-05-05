using System;
using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{
    public class LoginRequestDto
    {
        // Credential verification results (already verified by SSP)
        public VerifiedCredentialsDto VerifiedCredentials { get; set; }
        public string SamlSessionId { get; set; }
        public string OidcSessionId { get; set; }
        public Dictionary<string, string> AdditionalClaims { get; set; }
        public string LoginCompletedCallbackUrl { get; set; }
        public SspSettingsDto Settings { get; set; }
    }

    public class VerifiedCredentialsDto
    {
        public bool IsAuthenticated { get; set; }
        public bool IsBypass { get; set; }
        public bool UserMustChangePassword { get; set; }
        public DateTime? PasswordExpirationDate { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Username { get; set; }
        public string UserPrincipalName { get; set; }
        public string CustomIdentity { get; set; }
        public string Reason { get; set; }
    }
}