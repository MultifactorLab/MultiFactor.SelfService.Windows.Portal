using System;
using System.Configuration;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Sections
{
    public class PasswordRequirementsSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        private PasswordRequirementElementCollection Settings => (PasswordRequirementElementCollection)this[""];

        public static PasswordRequirements GetRequirements()
        {
            var section = (PasswordRequirementsSection)ConfigurationManager.GetSection("passwordRequirements");
            return section?.ToPasswordRequirements() ?? new PasswordRequirements();
        }

        private PasswordRequirements ToPasswordRequirements()
        {
            var requirements = new PasswordRequirements
            {
                RequiresUpperCaseLetters = GetBoolSetting("requires-upper-case-letters"),
                RequiresLowerCaseLetters = GetBoolSetting("requires-lower-case-letters"),
                RequiresDigits = GetBoolSetting("requires-digits"),
                RequiresSpecialSymbol = GetBoolSetting("requires-special-symbol")
            };

            ParsePasswordLength(requirements);
            
            return requirements;
        }
        
        private bool GetBoolSetting(string key)
        {
            var value = Settings[key]?.Value;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            
            bool.TryParse(value, out bool result);
            return result;
        }
        
        private void ParsePasswordLength(PasswordRequirements requirements)
        {
            requirements.MinLength = ParseLengthSetting("requires-min-password-length");
            requirements.MaxLength = ParseLengthSetting("requires-max-password-length");
    
            ValidatePasswordLengthConstraints(requirements);
        }

        private int ParseLengthSetting(string settingKey)
        {
            var lengthSetting = Settings[settingKey]?.Value;
    
            if (string.IsNullOrEmpty(lengthSetting))
            {
                return 0;
            }
    
            if (int.TryParse(lengthSetting, out int length) && length >= 0)
            {
                return length;
            }
    
            throw new ConfigurationErrorsException($"Configuration error: \"{settingKey}\" must be a positive integer");
        }

        private void ValidatePasswordLengthConstraints(PasswordRequirements requirements)
        {
            var hasMinLength = requirements.MinLength > 0;
            var hasMaxLength = requirements.MaxLength > 0;
    
            if (hasMinLength && hasMaxLength && requirements.MinLength > requirements.MaxLength)
            {
                throw new ConfigurationErrorsException(
                    "Configuration error: minimum password length cannot be greater than maximum password length");
            }
        }
    }

    public class PasswordRequirementElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PasswordRequirementElement();
        }
        
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PasswordRequirementElement)element).Key;
        }
        
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override string ElementName => "add";

        public new PasswordRequirementElement this[string key] => (PasswordRequirementElement)BaseGet(key);
    }

    
    public class PasswordRequirementElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        public string Key => (string)this["key"];


        [ConfigurationProperty("value", IsRequired = true)]
        public string Value => (string)this["value"];
    }
}