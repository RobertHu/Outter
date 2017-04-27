using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace SystemController.Config
{
    public sealed class CommandUrlSection : ConfigurationSection
    {
        private static CommandUrlSection _Settings = ConfigurationManager.GetSection("CommandCollectSection") as CommandUrlSection ?? new CommandUrlSection();

        public static CommandUrlSection GetConfig() { return _Settings; }
        [ConfigurationProperty("CommandUrls")]
        public UrlElementCollection CommandUrls
        {
            get { return (UrlElementCollection)this["CommandUrls"] ?? new UrlElementCollection(); }
        }
    }


    public sealed class UrlElementCollection : ConfigurationElementCollection
    {
        public UrlConfigElement this[int index]
        {
            get { return BaseGet(index) as UrlConfigElement; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new UrlConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UrlConfigElement)element).Url;
        }
    }

    public sealed class UrlConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url
        {
            get { return this["Url"] as string; }
        }

        [ConfigurationProperty("AppType", IsRequired = true)]
        public string AppType
        {
            get { return this["AppType"] as string; }
        }
    }
}
