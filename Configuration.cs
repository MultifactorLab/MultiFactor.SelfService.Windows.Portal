﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;

namespace MultiFactor.SelfService.Windows.Portal
{
    using static MultiFactor.SelfService.Windows.Portal.Constants;
    using ConfigurationConstants = Constants.Configuration;
    public class Configuration
    {
        public static Configuration Current { get; private set; }

        /// <summary>
        /// Company Name
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Active Directory Domain
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Only members of this group required to pass 2fa to access (Optional)
        /// </summary>
        public string ActiveDirectory2FaGroup { get; set; }

        /// <summary>
        /// Use ActiveDirectory User general properties phone number (Optional)
        /// </summary>
        public bool UseActiveDirectoryUserPhone { get; set; }

        /// <summary>
        /// Use ActiveDirectory User general properties mobile phone number (Optional)
        /// </summary>
        public bool UseActiveDirectoryMobileUserPhone { get; set; }

        /// <summary>
        /// Active Directory NetBIOS Name to add to login
        /// </summary>
        public string NetBiosName { get; set; }

        /// <summary>
        /// Only UPN user name format permitted
        /// </summary>
        public bool RequiresUpn { get; set; }

        //Lookup for UPN and use it instead of uid
        public bool UseUpnAsIdentity { get; set; }

        /// <summary>
        /// Use only these domains within forest(s)
        /// </summary>
        public IList<string> IncludedDomains { get; set; }

        /// <summary>
        /// Use all but not these domains within forest(s)
        /// </summary>
        public IList<string> ExcludedDomains { get; set; }

        /// <summary>
        /// Check if any included domains or exclude domains specified and contains required domain
        /// </summary>
        public bool IsPermittedDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));

            if (IncludedDomains?.Count > 0)
            {
                return IncludedDomains.Any(included => included.ToLower() == domain.ToLower());
            }
            if (ExcludedDomains?.Count > 0)
            {
                return !ExcludedDomains.Any(excluded => excluded.ToLower() == domain.ToLower());
            }

            return true;
        }

        /// <summary>
        /// Company Logo URL
        /// </summary>
        public string LogoUrl { get; set; }

        /// <summary>
        /// Multifactor API URL
        /// </summary>
        public string MultiFactorApiUrl { get; set; }

        /// <summary>
        /// HTTP Proxy for API
        /// </summary>
        public string MultiFactorApiProxy { get; set; }

        /// <summary>
        /// Multifactor API KEY
        /// </summary>
        public string MultiFactorApiKey { get; set; }

        /// <summary>
        /// Multifactor API Secret
        /// </summary>
        public string MultiFactorApiSecret { get; set; }

        /// <summary>
        /// Logging level
        /// </summary>
        public string LogLevel { get; set; }

        public bool EnablePasswordManagement { get; set; }
        public bool EnablePasswordRecovery { get; set; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; set; }

        public bool EnableCaptcha { get; private set; }
        public CaptchaType CaptchaType { get; private set; } = CaptchaType.Google;
        public string CaptchaKey { get; private set; }
        public string CaptchaSecret { get; private set; }

        public RequireCaptcha RequireCaptcha { get; private set; }

        public bool RequireCaptchaOnLogin => EnableCaptcha && RequireCaptcha == RequireCaptcha.Always;
        public bool CaptchaConfigured => EnableCaptcha;
        public bool IsCaptchaEnabled(CaptchaType type) => EnableCaptcha && CaptchaType == type;
        public string CaptchaProxy { get; private set; }

        public string SignUpGroups { get; private set; }

        public TimeSpan? PwdChangingSessionLifetime { get; private set; }
        public long? PwdChangingSessionCacheSize { get; private set; }

        public static void Load()
        {
            var appSettings = PortalSettings;

            if (appSettings == null)
            {
                throw new ConfigurationErrorsException("Can't find <portalSettings> element in web.config");
            }

            var companyNameSetting = GetRequiredValue(appSettings, ConfigurationConstants.General.COMPANY_NAME);
            var domainSetting = GetRequiredValue(appSettings, ConfigurationConstants.General.COMPANY_DOMAIN);
            var activeDirectory2FaGroupSetting = GetValue(appSettings, ConfigurationConstants.General.ACTIVE_DIRECTORY_2FA_GROUP);
            var domainNetBiosNameSetting = GetValue(appSettings, ConfigurationConstants.General.COMPANY_DOMAIN_NETBIOS_NAME);
            var logoUrlSetting = GetRequiredValue(appSettings, ConfigurationConstants.General.COMPANY_LOGO_URL);
            var apiUrlSetting = GetRequiredValue(appSettings, ConfigurationConstants.General.MULTIFACTOR_API_URL);
            var apiKeySetting = GetRequiredValue(appSettings, ConfigurationConstants.General.MULTIFACTOR_API_KEY);
            var apiProxySetting = GetValue(appSettings, ConfigurationConstants.General.MULTIFACTOR_API_PROXY);
            var apiSecretSetting = GetRequiredValue(appSettings, ConfigurationConstants.General.MULTIFACTOR_API_SECRET);
            var logLevelSetting = GetRequiredValue(appSettings, ConfigurationConstants.General.LOGGING_LEVEL);
           
            var useActiveDirectoryUserPhoneSetting = ParseBoolean(appSettings, ConfigurationConstants.General.USE_ACTIVE_DIRECTORY_USER_PHONE);
            var useActiveDirectoryMobileUserPhoneSetting = ParseBoolean(appSettings, ConfigurationConstants.General.USE_ACTIVE_DIRECTORY_MOBILE_USER_PHONE);
            var enablePasswordManagementSetting = ParseBoolean(appSettings, ConfigurationConstants.General.ENABLE_PASSWORD_MANAGEMENT);
            var enableExchangeActiveSyncSevicesManagementSetting = ParseBoolean(appSettings, ConfigurationConstants.General.ENABLE_EXCHANGE_ACTIVE_SYNC_DEVICES_MANAGEMENT);
            var useUpnAsIdentitySetting = ParseBoolean(appSettings, ConfigurationConstants.General.USE_UPN_AS_IDENTITY);

            var configuration = new Configuration
            {
                CompanyName = companyNameSetting,
                Domain = domainSetting,
                ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting,
                NetBiosName = domainNetBiosNameSetting,
                LogoUrl = logoUrlSetting,
                MultiFactorApiUrl = apiUrlSetting,
                MultiFactorApiKey = apiKeySetting,
                MultiFactorApiSecret = apiSecretSetting,
                MultiFactorApiProxy = apiProxySetting,
                LogLevel = logLevelSetting,
                EnableExchangeActiveSyncDevicesManagement = enableExchangeActiveSyncSevicesManagementSetting,
                EnablePasswordManagement = enablePasswordManagementSetting,
                UseActiveDirectoryUserPhone = useActiveDirectoryUserPhoneSetting,
                UseActiveDirectoryMobileUserPhone = useActiveDirectoryMobileUserPhoneSetting,
                UseUpnAsIdentity = useUpnAsIdentitySetting
            };

            var activeDirectorySection = (ActiveDirectorySection)ConfigurationManager.GetSection("ActiveDirectory");
            if (activeDirectorySection != null)
            {
                var includedDomains = (from object value in activeDirectorySection.IncludedDomains
                                       select ((ValueElement)value).Name).ToList();
                var excludeddDomains = (from object value in activeDirectorySection.ExcludedDomains
                                        select ((ValueElement)value).Name).ToList();

                if (includedDomains.Count > 0 && excludeddDomains.Count > 0)
                {
                    throw new Exception("Both IncludedDomains and ExcludedDomains configured.");
                }

                configuration.IncludedDomains = includedDomains;
                configuration.ExcludedDomains = excludeddDomains;
                configuration.RequiresUpn = activeDirectorySection.RequiresUpn;
            }


            if (!string.IsNullOrEmpty(appSettings[ConfigurationConstants.ObsoleteCaptcha.ENABLE_GOOGLE_RECAPTCHA]))
            {
                ReadObsoleteCaptchaSettings(appSettings, configuration);
            }
            else
            {
                ReadCaptchaSettings(appSettings, configuration);
            }

            configuration.CaptchaProxy = appSettings[ConfigurationConstants.Captcha.CAPTCHA_PROXY]; ;

            ReadSignUpGroupsSettings(appSettings, configuration);
            ReadAppCacheSettings(appSettings, configuration);
            ReadPasswordRecoverySettings(appSettings, configuration);
            Current = configuration;
        }

        private static bool ParseBoolean(NameValueCollection appSettings, string token)
        {
            var setting = GetValue(appSettings, token);
            if (!string.IsNullOrEmpty(setting))
            {
                if (!bool.TryParse(setting, out var value))
                {
                    throw new Exception($"Configuration error: Can't parse {token} value");
                }
                return value;
            }
            return default(bool);
        }

        private static string GetRequiredValue(NameValueCollection appSettings, string token)
        {
            var value = appSettings[token];
            if (string.IsNullOrEmpty(value))
            {
                throw new Exception($"Configuration error: {token} element not found or empty");
            }
            return value;
        }

        private static string GetValue(NameValueCollection appSettings, string token)
        {
            return appSettings[token];
        }

        private static void ReadObsoleteCaptchaSettings(NameValueCollection appSettings, Configuration configuration)
        {
            var enableGoogleReCaptchaSettings = appSettings[ConfigurationConstants.ObsoleteCaptcha.ENABLE_GOOGLE_RECAPTCHA];
            if (string.IsNullOrEmpty(enableGoogleReCaptchaSettings))
            {
                configuration.EnableCaptcha = false;
                return;
            }

            if (!bool.TryParse(enableGoogleReCaptchaSettings, out var enableGoogleReCaptcha))
            {
                throw new Exception($"Configuration error: Can't parse '{ConfigurationConstants.ObsoleteCaptcha.ENABLE_GOOGLE_RECAPTCHA}' value");
            }

            configuration.EnableCaptcha = enableGoogleReCaptcha;
            if (!enableGoogleReCaptcha) return;

            var googleReCaptchaKeySettings = appSettings[ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_KEY];
            var googleReCaptchaSecretSettings = appSettings[ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_SECRET];

            if (string.IsNullOrEmpty(googleReCaptchaKeySettings))
                throw new Exception(GetCaptchaError(ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_KEY));
            if (string.IsNullOrEmpty(googleReCaptchaSecretSettings))
                throw new Exception(GetCaptchaError(ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_SECRET));

            configuration.CaptchaKey = googleReCaptchaKeySettings;
            configuration.CaptchaSecret = googleReCaptchaSecretSettings;

            if (Enum.TryParse<RequireCaptcha>(appSettings[ConfigurationConstants.ObsoleteCaptcha.REQUIRE_CAPTCHA], true, out var rc))
            {
                configuration.RequireCaptcha = rc;
            }
            else
            {
                configuration.RequireCaptcha = enableGoogleReCaptcha
                    ? RequireCaptcha.Always
                    : RequireCaptcha.PasswordRecovery;
            }
        }

        private static void ReadCaptchaSettings(NameValueCollection appSettings, Configuration configuration)
        {
            var captchaEnabledSetting = appSettings[ConfigurationConstants.Captcha.ENABLE_CAPTCHA];
            if (string.IsNullOrEmpty(captchaEnabledSetting))
            {
                configuration.EnableCaptcha = false;
                return;
            }
            if (!bool.TryParse(captchaEnabledSetting, out var enableCaptcha))
            {
                throw new Exception($"Configuration error: Can't parse '{ConfigurationConstants.Captcha.ENABLE_CAPTCHA}' value");
            }
            configuration.EnableCaptcha = enableCaptcha;

            if (!enableCaptcha) return;

            if (Enum.TryParse<CaptchaType>(appSettings[ConfigurationConstants.Captcha.CAPTCHA_TYPE], true, out var ct))
            {
                configuration.CaptchaType = ct;
            }

            var captchaKeySetting = appSettings[ConfigurationConstants.Captcha.CAPTCHA_KEY];
            var captchaSecretSetting = appSettings[ConfigurationConstants.Captcha.CAPTCHA_SECRET];
            if (string.IsNullOrEmpty(captchaKeySetting)) throw new Exception(GetCaptchaError(ConfigurationConstants.Captcha.CAPTCHA_KEY));
            if (string.IsNullOrEmpty(captchaSecretSetting)) throw new Exception(GetCaptchaError(ConfigurationConstants.Captcha.CAPTCHA_SECRET));

            configuration.CaptchaKey = captchaKeySetting;
            configuration.CaptchaSecret = captchaSecretSetting;

            if (Enum.TryParse<RequireCaptcha>(appSettings[ConfigurationConstants.Captcha.REQUIRE_CAPTCHA], true, out var rc))
            {
                configuration.RequireCaptcha = rc;
            }
            else
            {
                configuration.RequireCaptcha = enableCaptcha
                    ? RequireCaptcha.Always
                    : RequireCaptcha.PasswordRecovery;
            }
        }

        private static void ReadPasswordRecoverySettings(NameValueCollection appSettings, Configuration configuration)
        {
            var enablePasswordRecoverySetting = appSettings[ConfigurationConstants.PasswordRecovery.ENABLE_PASSWORD_RECOVERY];

            if (!string.IsNullOrEmpty(enablePasswordRecoverySetting))
            {
                if (!bool.TryParse(enablePasswordRecoverySetting, out var enablePasswordRecovery))
                {
                    throw new Exception($"Configuration error: Can't parse '{ConfigurationConstants.PasswordRecovery.ENABLE_PASSWORD_RECOVERY}' value");
                }

                if (enablePasswordRecovery && !configuration.CaptchaConfigured)
                {
                    throw new Exception($"Configuration error: you need to enable captcha before using the password recovery feature");
                }

                configuration.EnablePasswordRecovery = enablePasswordRecovery;
            }
        }

        private static string GetCaptchaError(string elementName)
        {
            return $"Configuration error: '{elementName}' element not found or empty.\n" +
                $"Please check configuration file and define this property or disable captcha";
        }

        private static void ReadSignUpGroupsSettings(NameValueCollection appSettings, Configuration configuration)
        {
            const string signUpGroupsRegex = @"([\wа-я\s\-]+)(\s*;\s*([\wа-я\s\-]+)*)*";

            var signUpGroupsSettings = appSettings[ConfigurationConstants.SignUpGroups.SIGN_UP_GROUPS];
            if (string.IsNullOrWhiteSpace(signUpGroupsSettings))
            {
                configuration.SignUpGroups = string.Empty;
                return;
            }

            if (!Regex.IsMatch(signUpGroupsSettings, signUpGroupsRegex, RegexOptions.IgnoreCase))
            {
                throw new Exception($"Invalid group names. Please check '{ConfigurationConstants.SignUpGroups.SIGN_UP_GROUPS}' settings property and fix syntax errors.");
            }

            configuration.SignUpGroups = signUpGroupsSettings;
        }

        private static void ReadAppCacheSettings(NameValueCollection appSettings, Configuration configuration)
        {

            var pwdChangingSessionLifetimeSetting = appSettings[ConfigurationConstants.ChangingSessionCache.LIFETIME];
            if (!string.IsNullOrEmpty(pwdChangingSessionLifetimeSetting))
            {
                if (!TimeSpan.TryParseExact(pwdChangingSessionLifetimeSetting, @"hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out var timeSpan))
                {
                    throw new Exception($"Configuration error: Can't parse '{ConfigurationConstants.ChangingSessionCache.LIFETIME}' value");
                }

                configuration.PwdChangingSessionLifetime = timeSpan;
            }

            var pwdChangingSessionCacheSizeSettings = appSettings[ConfigurationConstants.ChangingSessionCache.SIZE];
            if (!string.IsNullOrEmpty(pwdChangingSessionCacheSizeSettings))
            {
                if (!long.TryParse(pwdChangingSessionCacheSizeSettings, out var bytes))
                {
                    throw new Exception($"Configuration error: Can't parse '{ConfigurationConstants.ChangingSessionCache.SIZE}' value");
                }

                configuration.PwdChangingSessionCacheSize = bytes;
            }
        }

        public static NameValueCollection PortalSettings
        {
            get
            {
                return ConfigurationManager.GetSection("portalSettings") as NameValueCollection;
            }
        }

        public static AuthenticationMode AuthenticationMode
        {
            get
            {
                var authenticationSection = WebConfigurationManager.GetSection("system.web/authentication") as AuthenticationSection;
                return authenticationSection?.Mode ?? AuthenticationMode.Forms;
            }
        }

        public static string GetLogFormat()
        {
            return PortalSettings?[ConfigurationConstants.General.LOGGING_FORMAT];
        }
    }

    public class ValueElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
        }
    }

    public class ValueElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ValueElement();
        }


        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ValueElement)element).Name;
        }
    }

    public class ActiveDirectorySection : ConfigurationSection
    {
        [ConfigurationProperty("ExcludedDomains", IsRequired = false)]
        public ValueElementCollection ExcludedDomains
        {
            get { return (ValueElementCollection)this["ExcludedDomains"]; }
        }

        [ConfigurationProperty("IncludedDomains", IsRequired = false)]
        public ValueElementCollection IncludedDomains
        {
            get { return (ValueElementCollection)this["IncludedDomains"]; }
        }

        [ConfigurationProperty("requiresUserPrincipalName", IsKey = false, IsRequired = false)]
        public bool RequiresUpn
        {
            get { return (bool)this["requiresUserPrincipalName"]; }
        }
    }

    public enum RequireCaptcha
    {
        Always,
        PasswordRecovery
    }

    public enum CaptchaType
    {
        Google = 0,
        Yandex = 1
    }
}