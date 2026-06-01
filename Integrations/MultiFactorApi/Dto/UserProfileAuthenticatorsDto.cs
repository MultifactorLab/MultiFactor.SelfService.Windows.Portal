using System.Linq;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserAuthenticatorsDto
    {
        public UserProfileAuthenticatorDto[] TotpAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] TelegramAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] MobileAppAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] PhoneAuthenticators { get; set; }

        public UserProfileAuthenticatorDto[] GetAuthenticators()
        {
            return Enumerable.Empty<UserProfileAuthenticatorDto>()
                .Concat(TotpAuthenticators ?? Enumerable.Empty<UserProfileAuthenticatorDto>())
                .Concat(TelegramAuthenticators ?? Enumerable.Empty<UserProfileAuthenticatorDto>())
                .Concat(MobileAppAuthenticators ?? Enumerable.Empty<UserProfileAuthenticatorDto>())
                .Concat(PhoneAuthenticators ?? Enumerable.Empty<UserProfileAuthenticatorDto>())
                .ToArray();
        }
    }
}