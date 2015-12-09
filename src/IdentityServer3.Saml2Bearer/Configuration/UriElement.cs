using System;
using System.Configuration;

namespace IdentityServer3.Saml2Bearer.Configuration
{
    public class UriElement : ConfigurationElement
    {
        private const string UriKey = "Uri";
        [ConfigurationProperty(UriKey, IsKey = true, IsRequired = true)]
        public Uri Uri
        {
            get { return new Uri(base[UriKey].ToString()); }
            set { base[UriKey] = value; }
        }
    }
}