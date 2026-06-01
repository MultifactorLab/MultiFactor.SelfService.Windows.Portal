namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserProfileAuthenticatorsApiDto
    {
        public UserProfileAuthenticatorDto[] TotpAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] TelegramAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] MobileAppAuthenticators { get; set; }
        public UserProfileAuthenticatorDto[] PhoneAuthenticators { get; set; }

        public UserProfileAuthenticatorsApiDto(
            UserProfileAuthenticatorDto[] totpAuthenticators,
            UserProfileAuthenticatorDto[] telegramAuthenticators,
            UserProfileAuthenticatorDto[] mobileAppAuthenticators,
            UserProfileAuthenticatorDto[] phoneAuthenticators)
        {
            TotpAuthenticators = totpAuthenticators;
            TelegramAuthenticators = telegramAuthenticators;
            MobileAppAuthenticators = mobileAppAuthenticators;
            PhoneAuthenticators = phoneAuthenticators;
        }
    }

    /// <summary>
    /// MFA authenticator
    /// </summary>
    public class UserProfileAuthenticatorDto
    {
        public string Id { get; set; }
        public string Label { get; set; }

        public UserProfileAuthenticatorDto(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }
}