namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{
    public class SspSettingsDto
    {
        public bool PreAuthenticationMethod { get; set; }
        public bool RequiresUserPrincipalName { get; set; }
        public bool PasswordManagementEnabled { get; set; }
        public bool NeedPrebindInfo { get; set; }
        public string PrivacyMode { get; set; } = "None";
        public string NetBiosName { get; set; }
        public string SignUpGroups { get; set; }
    }
}