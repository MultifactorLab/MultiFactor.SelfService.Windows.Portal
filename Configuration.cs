using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web.Configuration;

namespace MultiFactor.SelfService.Windows.Portal
{
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
        public bool EnableExchangeActiveSyncDevicesManagement { get; set; }

        public static void Load()
        {
            var appSettings = PortalSettings;

            if (appSettings == null)
            {
                throw new ConfigurationErrorsException("Can't find <portalSettings> element in web.config");
            }

            var companyNameSetting = appSettings["company-name"];
            var domainSetting = appSettings["company-domain"];
            var activeDirectory2FaGroupSetting = appSettings["active-directory-2fa-group"];
            var domainNetBiosNameSetting = appSettings["company-domain-netbios-name"];
            var logoUrlSetting = appSettings["company-logo-url"];
            var apiUrlSetting = appSettings["multifactor-api-url"];
            var apiKeySetting = appSettings["multifactor-api-key"];
            var apiProxySetting = appSettings["multifactor-api-proxy"];
            var apiSecretSetting = appSettings["multifactor-api-secret"];
            var logLevelSetting = appSettings["logging-level"];
            var useActiveDirectoryUserPhoneSetting = appSettings["use-active-directory-user-phone"];
            var useActiveDirectoryMobileUserPhoneSetting = appSettings["use-active-directory-mobile-user-phone"];
            var enablePasswordManagementSetting = appSettings["enable-password-management"];
            var enableExchangeActiveSyncSevicesManagementSetting = appSettings["enable-exchange-active-sync-devices-management"];
            var useUpnAsIdentitySetting = appSettings["use-upn-as-identity"];

            if (string.IsNullOrEmpty(companyNameSetting))
            {
                throw new Exception("Configuration error: 'company-name' element not found or empty");
            }
            if (string.IsNullOrEmpty(domainSetting))
            {
                throw new Exception("Configuration error: 'company-domain' element not found or empty");
            }
            if (string.IsNullOrEmpty(logoUrlSetting))
            {
                throw new Exception("Configuration error: 'company-logo-url' element not found or empty");
            }
            if (string.IsNullOrEmpty(apiUrlSetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-url' element not found or empty");
            }
            if (string.IsNullOrEmpty(apiKeySetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-key' element not found or empty");
            }
            if (string.IsNullOrEmpty(apiSecretSetting))
            {
                throw new Exception("Configuration error: 'multifactor-api-secret' element not found or empty");
            }
            if (string.IsNullOrEmpty(logLevelSetting))
            {
                throw new Exception("Configuration error: 'logging-level' element not found");
            }

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
                EnableExchangeActiveSyncDevicesManagement = false,
                EnablePasswordManagement = true
            };

            if (!string.IsNullOrEmpty(useActiveDirectoryUserPhoneSetting))
            {
                if (!bool.TryParse(useActiveDirectoryUserPhoneSetting, out var useActiveDirectoryUserPhone))
                {
                    throw new Exception("Configuration error: Can't parse 'use-active-directory-user-phone' value");
                }

                configuration.UseActiveDirectoryUserPhone = useActiveDirectoryUserPhone;
            }

            if (!string.IsNullOrEmpty(useActiveDirectoryMobileUserPhoneSetting))
            {
                if (!bool.TryParse(useActiveDirectoryMobileUserPhoneSetting, out var useActiveDirectoryMobileUserPhone))
                {
                    throw new Exception("Configuration error: Can't parse 'use-active-directory-mobile-user-phone' value");
                }

                configuration.UseActiveDirectoryMobileUserPhone = useActiveDirectoryMobileUserPhone;
            }

            if (!string.IsNullOrEmpty(enablePasswordManagementSetting))
            {
                if (!bool.TryParse(enablePasswordManagementSetting, out var enablePasswordManagement))
                {
                    throw new Exception("Configuration error: Can't parse 'enable-password-management' value");
                }
                configuration.EnablePasswordManagement = enablePasswordManagement;
            }

            if (!string.IsNullOrEmpty(enableExchangeActiveSyncSevicesManagementSetting))
            {
                if (!bool.TryParse(enableExchangeActiveSyncSevicesManagementSetting, out var enableExchangeActiveSyncSevicesManagement))
                {
                    throw new Exception("Configuration error: Can't parse 'enable-exchange-active-sync-devices-management' value");
                }
                configuration.EnableExchangeActiveSyncDevicesManagement = enableExchangeActiveSyncSevicesManagement;
            }

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

            if (bool.TryParse(useUpnAsIdentitySetting, out var useUpnAsIdentity))
            {
                configuration.UseUpnAsIdentity = useUpnAsIdentity;
            }

            ReadCaptchaSettings(appSettings, configuration);

            Current = configuration;
        }

        private static void ReadCaptchaSettings(NameValueCollection appSettings, Configuration configuration)
        {
            const string enabledCaptchaToken = "enable-google-re-captcha";
            const string captchaKeyToken = "google-re-captcha-key";
            const string captchaSecretToken = "google-re-captcha-secret";

            var enableGoogleReCaptchaSettings = appSettings[enabledCaptchaToken];
            if (string.IsNullOrEmpty(enableGoogleReCaptchaSettings))
            {
                configuration.EnableGoogleReCaptcha = false;
                return;
            }

            if (!bool.TryParse(enableGoogleReCaptchaSettings, out var enableGoogleReCaptcha))
            {
                throw new Exception($"Configuration error: Can't parse '{enabledCaptchaToken}' value");
            }

            configuration.EnableGoogleReCaptcha = enableGoogleReCaptcha;
            if (!enableGoogleReCaptcha) return;

            var googleReCaptchaKeySettings = appSettings["google-re-captcha-key"];
            var googleReCaptchaSecretSettings = appSettings["google-re-captcha-secret"];

            if (string.IsNullOrEmpty(googleReCaptchaKeySettings)) throw new Exception(GetCaptchaError(captchaKeyToken));
            if (string.IsNullOrEmpty(googleReCaptchaSecretSettings)) throw new Exception(GetCaptchaError(captchaSecretToken));

            configuration.GoogleReCaptchaKey = googleReCaptchaKeySettings;
            configuration.GoogleReCaptchaSecret = googleReCaptchaSecretSettings;
        }

        private static string GetCaptchaError(string elementName)
        {
            return $"Configuration error: '{elementName}' element not found or empty.\n" +
                $"Please check configuration file and define this property or disable captcha";
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

        public bool EnableGoogleReCaptcha { get; private set; }
        public string GoogleReCaptchaKey { get; private set; }
        public string GoogleReCaptchaSecret { get; private set; }

        public static string GetLogFormat()
        {
            return PortalSettings?["logging-format"];
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
}