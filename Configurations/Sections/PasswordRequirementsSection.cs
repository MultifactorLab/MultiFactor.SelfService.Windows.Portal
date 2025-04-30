using System;
using System.Collections.Generic;
using System.Configuration;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using MultiFactor.SelfService.Windows.Portal.Configurations.Validations;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Sections
{
    public class PasswordRequirementsSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        private PasswordRequirementElementCollection Settings => (PasswordRequirementElementCollection)this[""];

        public static PasswordRequirements GetRequirements()
        {
            var section = (PasswordRequirementsSection)ConfigurationManager.GetSection(Constants.Configuration.PasswordRequirements.SECTION_NAME);
            return section?.ToPasswordRequirements() ?? new PasswordRequirements();
        }

        private PasswordRequirements ToPasswordRequirements()
        {
            var validator = new PasswordRequirementsSectionValidator(Settings);
            var validConditions = Constants.Configuration.PasswordRequirements.GetAllKnownConstants();
            validator.Validate(validConditions);
            return InstantiateRequirements(validConditions);
        }

        private PasswordRequirements InstantiateRequirements(HashSet<string> validConditions)
        {
            var requirements = new PasswordRequirements();

            foreach (var condition in validConditions)
            {
                var element = Settings[condition];
                if (element != null)
                {
                    SetRequirementProperty(requirements, condition, element);
                }
            }

            requirements.NotifyingElements = Settings.GetElements(x => string.IsNullOrEmpty(x.Condition));
            return requirements;
        }

        private void SetRequirementProperty(PasswordRequirements requirements, string condition, PasswordRequirementElement element)
        {
            switch (condition)
            {
                case Constants.Configuration.PasswordRequirements.UPPER_CASE_LETTERS:
                    requirements.UpperCaseLetters = element;
                    break;
                case Constants.Configuration.PasswordRequirements.LOWER_CASE_LETTERS:
                    requirements.LowerCaseLetters = element;
                    break;
                case Constants.Configuration.PasswordRequirements.DIGITS:
                    requirements.Digits = element;
                    break;
                case Constants.Configuration.PasswordRequirements.SPECIAL_SYMBOLS:
                    requirements.SpecialSymbols = element;
                    break;
                case Constants.Configuration.PasswordRequirements.MIN_LENGTH:
                    requirements.MinLength = element;
                    break;
                case Constants.Configuration.PasswordRequirements.MAX_LENGTH:
                    requirements.MaxLength = element;
                    break;
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
            return (element as PasswordRequirementElement).Key;
        }
        
        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override string ElementName => "pwd-req";

        public new PasswordRequirementElement this[string key] => (PasswordRequirementElement)BaseGet(key);
        
        public IEnumerable<PasswordRequirementElement> GetElements(
            Func<PasswordRequirementElement, bool> predicate = null)
        {
            foreach (var item in this)
            {
                var element = (PasswordRequirementElement)item;
        
                if (predicate == null || predicate(element))
                {
                    yield return element;
                }
            }
        }

        public void Add(PasswordRequirementElement element)
        {
            BaseAdd(element);
        }
    }

    public class PasswordRequirementElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsKey = true)]
        public string Key
        {
            get => string.IsNullOrEmpty(Condition) ? Guid.NewGuid().ToString() : Condition;
            set => this["key"] = value;
        }
        
        [ConfigurationProperty("condition")]
        public string Condition 
        { 
            get => (string)this["condition"];
            set => this["condition"] = value;
        }

        [ConfigurationProperty("value")]
        public string Value 
        { 
            get => (string)this["value"];
            set => this["value"] = value;
        }

        [ConfigurationProperty("enabled", DefaultValue = false)]
        public bool Enabled 
        { 
            get => (bool)this["enabled"];
            set => this["enabled"] = value;
        }
        
        [ConfigurationProperty("descriptionEn")]
        public string DescriptionEn 
        { 
            get => (string)this["descriptionEn"];
            set => this["descriptionEn"] = value;
        }
        
        [ConfigurationProperty("descriptionRu")]
        public string DescriptionRu 
        { 
            get => (string)this["descriptionRu"];
            set => this["descriptionRu"] = value;
        }
    }
}