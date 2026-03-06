using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Services.API.DTO
{
    public class UserProfileAuthenticatorsDto
    {
        public UserProfileAuthenticatorDto[] TotpAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] TelegramAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] MobileAppAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] PhoneAuthenticators { get; set; }

        public UserProfileAuthenticatorDto[] GetAuthenticators()
        {
            return TotpAuthenticators
                .Concat(TelegramAuthenticators)
                .Concat(MobileAppAuthenticators)
                .Concat(PhoneAuthenticators)
                .ToArray();
        }
    }

    /// <summary>
    /// MFA authenticator
    /// </summary>
    public class UserProfileAuthenticatorDto
    {
        public string Id { get; set; }
        public string Label { get; set; }
    }
}