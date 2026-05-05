using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{

    /// <summary>
    /// Identity request DTO with optionally pre-verified membership data.
    /// Used for pre-authentication flow (MFA first, then password).
    /// </summary>
    public class IdentityRequestDto
    {
        public string Username { get; set; }
        public VerifiedMembershipDto VerifiedMembership { get; set; }
        public string SamlSessionId { get; set; }
        public string OidcSessionId { get; set; }
        public Dictionary<string, string> AdditionalClaims { get; set; }
        public string LoginCompletedCallbackUrl { get; set; }
        public IdentitySspSettingsDto Settings { get; set; }

        //public IdentityRequestDto(
        //    string username,
        //    string loginCompletedCallbackUrl,
        //    IdentitySspSettingsDto settings)
        //{
        //    Username = username;
        //    LoginCompletedCallbackUrl = loginCompletedCallbackUrl;
        //    Settings = settings;
        //}
    }

    /// <summary>
    /// Pre-verified membership from SSP's local AD verification (without password).
    /// Used when NeedPrebindInfo is true.
    /// </summary>
    public class VerifiedMembershipDto
    {
        public bool IsBypass { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string UserPrincipalName { get; set; }
        public string CustomIdentity { get; set; }

        //public VerifiedMembershipDto(bool isBypass)
        //{
        //    IsBypass = isBypass;
        //}
    }

    public class IdentitySspSettingsDto
    {
        public bool PreAuthenticationMethod { get; set; }
        public bool RequiresUserPrincipalName { get; set; }
        public bool NeedPrebindInfo { get; set; }
        public bool UseUpnAsIdentity { get; set; }

        public string PrivacyMode { get; set; } = "None";
        public string NetBiosName { get; set; }
        public string SignUpGroups { get; set; }
    }
}