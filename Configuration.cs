using System;
using System.Collections.Specialized;
using System.Configuration;

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
        /// Active Directory NetBIOS Name to add to login
        /// </summary>
        public string NetBiosName { get; set; }

        /// <summary>
        /// Company Logo URL
        /// </summary>
        public string LogoUrl { get; set; }

        /// <summary>
        /// Multifactor API URL
        /// </summary>
        public string MultiFactorApiUrl { get; set; }

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

        public static void Load()
        {
            var appSettings = ConfigurationManager.GetSection("portalSettings") as NameValueCollection;

            var companyNameSetting = appSettings["company-name"];
            var domainSetting = appSettings["company-domain"];
            var domainNetBiosNameSetting = appSettings["company-domain-netbios-name"];
            var logoUrlSetting = appSettings["company-logo-url"];
            var apiUrlSetting = appSettings["multifactor-api-url"];
            var apiKeySetting = appSettings["multifactor-api-key"];
            var apiSecretSetting = appSettings["multifactor-api-secret"];
            var logLevelSetting = appSettings["logging-level"];

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

            Current = new Configuration
            {
                CompanyName = companyNameSetting,
                Domain = domainSetting,
                NetBiosName = domainNetBiosNameSetting,
                LogoUrl = logoUrlSetting,
                MultiFactorApiUrl = apiUrlSetting,
                MultiFactorApiKey = apiKeySetting,
                MultiFactorApiSecret = apiSecretSetting,
                LogLevel = logLevelSetting
            };
        }
    }
}