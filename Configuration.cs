using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Models;

namespace MultiFactor.SelfService.Windows.Portal
{
    using ConfigurationConstants = Constants.Configuration;
    public class Configuration
    {
        public static Configuration Current { get; private set; }

        /// <summary>
        /// Company Name
        /// </summary>
        public string CompanyName { get; private set; }

        /// <summary>
        /// Active Directory Domain
        /// </summary>
        public string Domain { get; private set; }

        /// <summary>
        /// Only members of these groups required to pass 2fa to access (Optional)
        /// </summary>
        public string[] ActiveDirectory2FaGroup { get; private set; } = Array.Empty<string>();
        
        /// <summary>
        /// Only members of these groups have access to the resource (Optional)
        /// </summary>
        public string[] ActiveDirectoryGroup { get; private set; } = Array.Empty<string>();

        public bool LoadActiveDirectoryNestedGroups { get; private set; } = true;
        
        public string[] NestedGroupsBaseDn { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Use ActiveDirectory User general properties phone number (Optional)
        /// </summary>
        public bool UseActiveDirectoryUserPhone { get; private set; }

        /// <summary>
        /// Use ActiveDirectory User general properties mobile phone number (Optional)
        /// </summary>
        public bool UseActiveDirectoryMobileUserPhone { get; private set; }

        /// <summary>
        /// Active Directory NetBIOS Name to add to login
        /// </summary>
        public string NetBiosName { get; private set; }

        /// <summary>
        /// Only UPN user name format permitted
        /// </summary>
        public bool RequiresUpn { get; private set; }

        //Lookup for UPN and use it instead of uid
        public bool UseUpnAsIdentity { get; private set; }

        public bool NeedPrebindInfo()
        {
            return UseUpnAsIdentity || ActiveDirectory2FaGroup.Any() || EnablePasswordManagement;
        }

        /// <summary>
        /// Use only these domains within forest(s)
        /// </summary>
        private IList<string> IncludedDomains { get; set; }

        /// <summary>
        /// Use all but not these domains within forest(s)
        /// </summary>
        private IList<string> ExcludedDomains { get; set; }

        /// <summary>
        /// Check if any included domains or exclude domains specified and contains required domain
        /// </summary>
        public bool IsPermittedDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));

            if (IncludedDomains?.Count > 0)
            {
                return IncludedDomains.Any(included => string.Equals(included, domain, StringComparison.CurrentCultureIgnoreCase));
            }
            if (ExcludedDomains?.Count > 0)
            {
                return ExcludedDomains.All(excluded => !string.Equals(excluded, domain, StringComparison.CurrentCultureIgnoreCase));
            }

            return true;
        }

        /// <summary>
        /// Company Logo URL
        /// </summary>
        public string LogoUrl { get; private set; }

        /// <summary>
        /// Multifactor API URL
        /// </summary>
        public string MultiFactorApiUrl { get; private set; }

        /// <summary>
        /// HTTP Proxy for API
        /// </summary>
        public string MultiFactorApiProxy { get; private set; }

        /// <summary>
        /// Multifactor API KEY
        /// </summary>
        public string MultiFactorApiKey { get; private set; }

        /// <summary>
        /// Multifactor API Secret
        /// </summary>
        public string MultiFactorApiSecret { get; private set; }
        
        public bool PreAuthnMode { get; private set; }
        
        /// <summary>
        /// Logging level
        /// </summary>
        public string LogLevel { get; private set; }

        public bool EnablePasswordManagement { get; private set; }
        public bool EnablePasswordRecovery { get; private set; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; private set; }

        private bool EnableCaptcha { get; set; }
        private CaptchaType CaptchaType { get; set; } = CaptchaType.Google;
        public string CaptchaKey { get; private set; }
        public string CaptchaSecret { get; private set; }

        private RequireCaptcha RequireCaptcha { get; set; }

        public bool RequireCaptchaOnLogin => EnableCaptcha && RequireCaptcha == RequireCaptcha.Always;
        public bool CaptchaConfigured => EnableCaptcha;
        public bool IsCaptchaEnabled(CaptchaType type) => EnableCaptcha && CaptchaType == type;
        public string CaptchaProxy { get; private set; }

        public string SignUpGroups { get; private set; }

        public TimeSpan? PwdChangingSessionLifetime { get; private set; }
        public long? PwdChangingSessionCacheSize { get; private set; }
        public Link[] Links { get; private set; }
        public int NotifyOnPasswordExpirationDaysLeft { get; private set; }
        
        public PrivacyModeDescriptor PrivacyModeDescriptor { get; private set; }

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
            var preAuthnMode = ParseBoolean(appSettings, ConfigurationConstants.General.PRE_AUTHN_MODE);

            var useActiveDirectoryUserPhoneSetting = ParseBoolean(appSettings, ConfigurationConstants.General.USE_ACTIVE_DIRECTORY_USER_PHONE);
            var useActiveDirectoryMobileUserPhoneSetting = ParseBoolean(appSettings, ConfigurationConstants.General.USE_ACTIVE_DIRECTORY_MOBILE_USER_PHONE);
            var enablePasswordManagementSetting = ParseBoolean(appSettings, ConfigurationConstants.General.ENABLE_PASSWORD_MANAGEMENT);
            var enableExchangeActiveSyncServicesManagementSetting = ParseBoolean(appSettings, ConfigurationConstants.General.ENABLE_EXCHANGE_ACTIVE_SYNC_DEVICES_MANAGEMENT);
            var useUpnAsIdentitySetting = ParseBoolean(appSettings, ConfigurationConstants.General.USE_UPN_AS_IDENTITY);
            var notifyPasswordExpirationDaysLeft = ReadNotifyPasswordExpirationDaysLeft(appSettings);

            var loadActiveDirectoryNestedGroups = ParseBoolean(appSettings, ConfigurationConstants.General.LOAD_AD_NESTED_GROUPS);
            var activeDirectoryGroupSetting = GetValue(appSettings, ConfigurationConstants.General.ACTIVE_DIRECTORY_GROUP);
            var nestedGroupsBaseDn = GetValue(appSettings, ConfigurationConstants.General.NESTED_GROUPS_BASE_DN);
       
            var privacyMode = GetValue(appSettings, ConfigurationConstants.General
            var configuration = new Configuration
            {
                CompanyName = companyNameSetting,
                Domain = domainSetting,
                NetBiosName = domainNetBiosNameSetting,
                LogoUrl = logoUrlSetting,
                MultiFactorApiUrl = apiUrlSetting,
                MultiFactorApiKey = apiKeySetting,
                MultiFactorApiSecret = apiSecretSetting,
                MultiFactorApiProxy = apiProxySetting,
                LogLevel = logLevelSetting,
                EnableExchangeActiveSyncDevicesManagement = enableExchangeActiveSyncServicesManagementSetting,
                EnablePasswordManagement = enablePasswordManagementSetting,
                UseActiveDirectoryUserPhone = useActiveDirectoryUserPhoneSetting,
                UseActiveDirectoryMobileUserPhone = useActiveDirectoryMobileUserPhoneSetting,
                UseUpnAsIdentity = useUpnAsIdentitySetting,
                NotifyOnPasswordExpirationDaysLeft = notifyPasswordExpirationDaysLeft,
                PreAuthnMode = preAuthnMode,
                LoadActiveDirectoryNestedGroups = loadActiveDirectoryNestedGroups
                PrivacyModeDescriptor = PrivacyModeDescriptor.Create(privacyMode)
            };
            
            if (!string.IsNullOrEmpty(activeDirectory2FaGroupSetting))
            {
                configuration.ActiveDirectory2FaGroup = activeDirectory2FaGroupSetting
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .ToArray();
            }
            
            if (!string.IsNullOrEmpty(nestedGroupsBaseDn))
            {
                configuration.NestedGroupsBaseDn = nestedGroupsBaseDn
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            if (!string.IsNullOrEmpty(activeDirectoryGroupSetting))
            {
                configuration.ActiveDirectoryGroup = activeDirectoryGroupSetting
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .ToArray();
            }

            var activeDirectorySection = (ActiveDirectorySection)ConfigurationManager.GetSection("ActiveDirectory");
            if (activeDirectorySection != null)
            {
                var includedDomains = (from object value in activeDirectorySection.IncludedDomains
                                       select ((ValueElement)value).Name).ToList();
                var excludedDomains = (from object value in activeDirectorySection.ExcludedDomains
                                        select ((ValueElement)value).Name).ToList();

                if (includedDomains.Count > 0 && excludedDomains.Count > 0)
                {
                    throw new ConfigurationErrorsException("Both IncludedDomains and ExcludedDomains configured.");
                }

                configuration.IncludedDomains = includedDomains;
                configuration.ExcludedDomains = excludedDomains;
                configuration.RequiresUpn = activeDirectorySection.RequiresUpn;
            }

            var linkShowcaseSection = (LinkShowcaseSection)ConfigurationManager.GetSection("linksShowcase");
            if (linkShowcaseSection != null)
            {
                configuration.Links = (from object value in linkShowcaseSection.Links
                                       select new Link((LinkElement)value)).ToArray();
            }

            if (!string.IsNullOrEmpty(appSettings[ConfigurationConstants.ObsoleteCaptcha.ENABLE_GOOGLE_RECAPTCHA]))
            {
                ReadObsoleteCaptchaSettings(appSettings, configuration);
            }
            else
            {
                ReadCaptchaSettings(appSettings, configuration);
            }

            configuration.CaptchaProxy = appSettings[ConfigurationConstants.Captcha.CAPTCHA_PROXY];
            
            ReadSignUpGroupsSettings(appSettings, configuration);
            ReadAppCacheSettings(appSettings, configuration);
            ReadPasswordRecoverySettings(appSettings, configuration);
            Current = configuration;
        }

        private static bool ParseBoolean(NameValueCollection appSettings, string token)
        {
            var setting = GetValue(appSettings, token);
            if (string.IsNullOrEmpty(setting)) return default;
            if (!bool.TryParse(setting, out var value))
            {
                throw new ConfigurationErrorsException($"Configuration error: Can't parse {token} value");
            }
            return value;
        }

        private static string GetRequiredValue(NameValueCollection appSettings, string token)
        {
            var value = appSettings[token];
            if (string.IsNullOrEmpty(value))
            {
                throw new ConfigurationErrorsException($"Configuration error: {token} element not found or empty");
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
                throw new ConfigurationErrorsException($"Configuration error: Can't parse '{ConfigurationConstants.ObsoleteCaptcha.ENABLE_GOOGLE_RECAPTCHA}' value");
            }

            configuration.EnableCaptcha = enableGoogleReCaptcha;
            if (!enableGoogleReCaptcha) return;

            var googleReCaptchaKeySettings = appSettings[ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_KEY];
            var googleReCaptchaSecretSettings = appSettings[ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_SECRET];

            if (string.IsNullOrEmpty(googleReCaptchaKeySettings))
                throw new ConfigurationErrorsException(GetCaptchaError(ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_KEY));
            if (string.IsNullOrEmpty(googleReCaptchaSecretSettings))
                throw new ConfigurationErrorsException(GetCaptchaError(ConfigurationConstants.ObsoleteCaptcha.GOOGLE_RECAPTCHA_SECRET));

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
                throw new ConfigurationErrorsException($"Configuration error: Can't parse '{ConfigurationConstants.Captcha.ENABLE_CAPTCHA}' value");
            }
            configuration.EnableCaptcha = enableCaptcha;

            if (!enableCaptcha) return;

            if (Enum.TryParse<CaptchaType>(appSettings[ConfigurationConstants.Captcha.CAPTCHA_TYPE], true, out var ct))
            {
                configuration.CaptchaType = ct;
            }

            var captchaKeySetting = appSettings[ConfigurationConstants.Captcha.CAPTCHA_KEY];
            var captchaSecretSetting = appSettings[ConfigurationConstants.Captcha.CAPTCHA_SECRET];
            if (string.IsNullOrEmpty(captchaKeySetting)) throw new ConfigurationErrorsException(GetCaptchaError(ConfigurationConstants.Captcha.CAPTCHA_KEY));
            if (string.IsNullOrEmpty(captchaSecretSetting)) throw new ConfigurationErrorsException(GetCaptchaError(ConfigurationConstants.Captcha.CAPTCHA_SECRET));

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
                    throw new ConfigurationErrorsException($"Configuration error: Can't parse '{ConfigurationConstants.PasswordRecovery.ENABLE_PASSWORD_RECOVERY}' value");
                }

                if (enablePasswordRecovery && !configuration.CaptchaConfigured)
                {
                    throw new ConfigurationErrorsException($"Configuration error: you need to enable captcha before using the password recovery feature");
                }

                configuration.EnablePasswordRecovery = enablePasswordRecovery;
            }
        }

        private static string GetCaptchaError(string elementName)
        {
            return $"Configuration error: '{elementName}' element not found or empty.\n" +
                $"Please check configuration file and define this property or disable captcha";
        }

        private static int ReadNotifyPasswordExpirationDaysLeft(NameValueCollection appSettings)
        {
            var notifyPasswordExpirationDaysLeft = GetValue(appSettings, ConfigurationConstants.General.NOTIFY_PASSWORD_EXPIRATION_DAYS_LEFT);
            if (notifyPasswordExpirationDaysLeft != null)
            {
                var notifyPasswordExpirationDaysLeftInt = int.Parse(notifyPasswordExpirationDaysLeft);
                if(notifyPasswordExpirationDaysLeftInt < 0 || notifyPasswordExpirationDaysLeftInt > 365)
                {
                    throw new ConfigurationErrorsException("'notify-on-password-expiration-days-left' must be in range between 0 and 365");
                }
                return notifyPasswordExpirationDaysLeftInt;
            }
            return 0;
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
                throw new ConfigurationErrorsException($"Invalid group names. Please check '{ConfigurationConstants.SignUpGroups.SIGN_UP_GROUPS}' settings property and fix syntax errors.");
            }

            configuration.SignUpGroups = signUpGroupsSettings;
        }

        private static void ReadAppCacheSettings(NameValueCollection appSettings, Configuration configuration)
        {

            var pwdChangingSessionLifetimeSetting = appSettings[ConfigurationConstants.ChangingSessionCache.LIFETIME];
            if (!string.IsNullOrEmpty(pwdChangingSessionLifetimeSetting))
            {
                if (!TimeSpan.TryParseExact(pwdChangingSessionLifetimeSetting, @"hh\:mm\:ss", null, TimeSpanStyles.None, out var timeSpan))
                {
                    throw new ConfigurationErrorsException($"Configuration error: Can't parse '{ConfigurationConstants.ChangingSessionCache.LIFETIME}' value");
                }

                configuration.PwdChangingSessionLifetime = timeSpan;
            }

            var pwdChangingSessionCacheSizeSettings = appSettings[ConfigurationConstants.ChangingSessionCache.SIZE];
            if (!string.IsNullOrEmpty(pwdChangingSessionCacheSizeSettings))
            {
                if (!long.TryParse(pwdChangingSessionCacheSizeSettings, out var bytes))
                {
                    throw new ConfigurationErrorsException($"Configuration error: Can't parse '{ConfigurationConstants.ChangingSessionCache.SIZE}' value");
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

    public class LinkShowcaseSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public LinkShowcaseElementCollection Links
        {
            get { return (LinkShowcaseElementCollection)this[""]; }
        }
    }


    public class LinkShowcaseElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new LinkElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as LinkElement).Url;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "link"; }
        }
    }

    public class LinkElement : ConfigurationElement
    {
        [ConfigurationProperty("url", IsRequired = true, IsKey = true)]
        public string Url { get { return (string)this["url"]; } }

        [ConfigurationProperty("title", IsRequired = true)]
        public string Title { get { return (string)this["title"]; } }

        [ConfigurationProperty("image", IsRequired = true)]
        public string Image { get { return (string)this["image"]; } }

        [ConfigurationProperty("newTab", IsRequired = false, DefaultValue = true)]
        public bool OpenInNewTab 
        {
            get
            {
                return (bool)this["newTab"]; 
            }
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