namespace MultiFactor.SelfService.Windows.Portal.Services.API.DTO
{
    /// <summary>
    /// User profile
    /// </summary>
    public class UserProfile
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public bool EnablePasswordManagement { get; set; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; set; }
        public int PasswordExpirationDaysLeft { get; set; }
    }
}
