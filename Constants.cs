namespace MultiFactor.SelfService.Windows.Portal
{
    public class Constants
    {
        public const string COOKIE_NAME = "multifactor";
        public const string SESSION_EXPIRED_PASSWORD_USER_KEY = "multifactor:expired-password:user";
        public const string SESSION_EXPIRED_PASSWORD_CIPHER_KEY = "multifactor:expired-password:cipher";
        public const string CAPTCHA_TOKEN = "responseToken";
        public const string PWD_RECOVERY_COOKIE = "PSession";


        public static class Configuration
        {
            public static class General
            {
                public const string COMPANY_NAME = "company-name";
                public const string COMPANY_DOMAIN = "company-domain";
                public const string ACTIVE_DIRECTORY_2FA_GROUP = "active-directory-2fa-group";
                public const string COMPANY_DOMAIN_NETBIOS_NAME = "company-domain-netbios-name";
                public const string COMPANY_LOGO_URL = "company-logo-url";
                public const string MULTIFACTOR_API_URL = "multifactor-api-url";
                public const string MULTIFACTOR_API_KEY = "multifactor-api-key";
                public const string MULTIFACTOR_API_PROXY = "multifactor-api-proxy";
                public const string MULTIFACTOR_API_SECRET = "multifactor-api-secret";
                public const string LOGGING_LEVEL = "logging-level";
                public const string USE_ACTIVE_DIRECTORY_USER_PHONE = "use-active-directory-user-phone";
                public const string USE_ACTIVE_DIRECTORY_MOBILE_USER_PHONE = "use-active-directory-mobile-user-phone";
                public const string ENABLE_PASSWORD_MANAGEMENT = "enable-password-management";
                public const string ENABLE_EXCHANGE_ACTIVE_SYNC_DEVICES_MANAGEMENT = "enable-exchange-active-sync-devices-management";
                public const string USE_UPN_AS_IDENTITY = "use-upn-as-identity";
                public const string LOGGING_FORMAT = "logging-format";
                public const string NOTIFY_PASSWORD_EXPIRATION_DAYS_LEFT = "notify-on-password-expiration-days-left";
            }

            public static class ObsoleteCaptcha
            {
                public const string ENABLE_GOOGLE_RECAPTCHA = "enable-google-re-captcha";
                public const string GOOGLE_RECAPTCHA_KEY = "google-re-captcha-key";
                public const string GOOGLE_RECAPTCHA_SECRET = "google-re-captcha-secret";
                public const string REQUIRE_CAPTCHA = "require-captcha";
            }

            public static class Captcha
            {
                public const string ENABLE_CAPTCHA = "enable-captcha";
                public const string CAPTCHA_TYPE = "captcha-type";
                public const string CAPTCHA_KEY = "captcha-key";
                public const string CAPTCHA_SECRET = "captcha-secret";
                public const string REQUIRE_CAPTCHA = "require-captcha";
                public const string CAPTCHA_PROXY = "captcha-proxy";
            }

            public static class PasswordRecovery
            {
                public const string ENABLE_PASSWORD_RECOVERY = "enable-password-recovery";
            }

            public static class SignUpGroups
            {
                public const string SIGN_UP_GROUPS = "sign-up-groups";
            }

            public static class ChangingSessionCache
            {
                public const string LIFETIME = "pwd-changing-session-lifetime";
                public const string SIZE = "pwd-changing-session-cache-size";
            }
        }
    }
}