using System.Configuration;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Sections
{
    public class PasswordRequirementsSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        private PasswordRequirementElementCollection Settings => (PasswordRequirementElementCollection)this[""];

        public PasswordRequirements ToPasswordRequirements()
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
            var lengthPolicy = Settings["requires-password-length"]?.Value;
            if (!string.IsNullOrEmpty(lengthPolicy))
            {
                var parts = lengthPolicy.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                {
                    requirements.MinLength = min;
                    requirements.MaxLength = max;
                }
                else
                {
                    throw new ConfigurationErrorsException("Configuration error: 'requires-password-length' must be in format 'min-max', for example '8-20'");
                }
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