using System.Configuration;

namespace IdentityServer3.Contrib.Saml2Bearer.Configuration
{
    public class AudienceElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new UriElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UriElement)element).Uri;
        }
    }
}