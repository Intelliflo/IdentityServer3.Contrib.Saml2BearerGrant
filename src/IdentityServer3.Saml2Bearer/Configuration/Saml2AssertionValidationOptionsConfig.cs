using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace IdentityServer3.Saml2Bearer.Configuration
{
    public class Saml2AssertionValidationOptionsConfig : ConfigurationSection, ISaml2AssertionValidationOptions
    {
        private Uri recipient;
        public const string SectionXPath = "Saml2AssertionValidationConfiguration";

        public static ISaml2AssertionValidationOptions GetSection()
        {
            var config = (ISaml2AssertionValidationOptions)ConfigurationManager.GetSection(SectionXPath);
            if (config == null)
                throw new ArgumentNullException("Could not read Saml2AssertionValidationConfiguration section");
            return config;
        }

        private const string RecipientKey = "Recipient";
        [ConfigurationProperty(RecipientKey, IsRequired = false)]
        public virtual Uri Recipient
        {
            get
            {
                if (recipient == null)
                    return this[RecipientKey] as Uri;
                return recipient;
            }
            set { recipient = value; }
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
}
