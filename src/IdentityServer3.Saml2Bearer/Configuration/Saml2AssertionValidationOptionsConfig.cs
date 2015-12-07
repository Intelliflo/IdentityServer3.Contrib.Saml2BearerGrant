using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace IdentityServer3.Saml2Bearer.Configuration
{
    public class Saml2AssertionValidationOptionsConfig : ConfigurationSection, ISaml2AssertionValidationOptions
    {
        public const string SectionXPath = "Saml2AssertionValidationConfiguration";

        public static ISaml2AssertionValidationOptions GetSection()
        {
            var config = (ISaml2AssertionValidationOptions)ConfigurationManager.GetSection(SectionXPath);
            if (config == null)
                throw new ArgumentNullException("Could not read Saml2AssertionValidationConfiguration section");
            return config;
        }

        private const string RecipientKey = "Recipient";
        [ConfigurationProperty(RecipientKey, IsRequired = true)]
        public virtual Uri Recipient
        {
            get { return this[RecipientKey] as Uri; }
            set { this[RecipientKey] = value; }
        }

        public IList<Uri> Audience
        {
            get
            {
                var list = new List<Uri>();
                foreach (var x in AudienceCollection)
                {
                    var u = x as UriElement;
                    list.Add(u.Uri);
                }
                return list;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private const string AudienceKey = "Audience";
        [ConfigurationProperty(AudienceKey, IsRequired = true, IsDefaultCollection = true)]
        public AudienceElementCollection AudienceCollection
        {
            get { return (AudienceElementCollection)this[AudienceKey]; }
            set { this[AudienceKey] = value; }
        }

        /// <summary>
        /// Set this manually
        /// </summary>
        public X509Certificate2 Certificate { get; set; }
    }

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
