namespace MultiFactor.SelfService.Windows.Portal.Services.API
{
    public static class MultiFactorClaims
    {
        public const string SamlSessionId = "samlSessionId";
        public const string OidcSessionId = "oidcSessionId";
        public const string ChangePassword = "changePassword";
        public const string PasswordExpirationDate = "passwordExpirationDate";
        public const string ResetPassword = "resetPassword";
        public const string RawUserName = "rawUserName";
    }
}