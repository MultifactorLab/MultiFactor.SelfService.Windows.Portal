namespace MultiFactor.SelfService.Windows.Portal.Services.API.DTO
{
    /// <summary>
    /// User group policy 
    /// </summary>
    public class UserProfilePolicy
    {
        public bool Totp { get; set; }
        public bool Telegram { get; set; }
        public bool MobileApp { get; set; }
        public bool Phone { get; set; }
    }
}