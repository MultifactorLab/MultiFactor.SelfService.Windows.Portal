using System.Configuration;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Sections
{
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
}