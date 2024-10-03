using System;
using System.Collections.Generic;

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

        public IList<UserProfileAuthenticator> TotpAuthenticators { get; set; }
        public IList<UserProfileAuthenticator> TelegramAuthenticators { get; set; }
        public IList<UserProfileAuthenticator> MobileAppAuthenticators { get; set; }
        public IList<UserProfileAuthenticator> PhoneAuthenticators { get; set; }

        public int Count =>
            TotpAuthenticators.Count +
            TelegramAuthenticators.Count +
            MobileAppAuthenticators.Count +
            PhoneAuthenticators.Count;

        public UserProfilePolicy Policy { get; set; }

        public bool EnablePasswordManagement { get; set; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; set; }
        public int PasswordExpirationDaysLeft { get; set; }
    }
}