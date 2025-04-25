using System.Configuration;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Sections
{
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
    
    public class ValueElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
        }
    }
}