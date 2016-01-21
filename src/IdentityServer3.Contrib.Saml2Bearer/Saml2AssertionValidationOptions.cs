using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace IdentityServer3.Contrib.Saml2Bearer
{
    public class Saml2AssertionValidationOptions : ISaml2AssertionValidationOptions
    {
        public Saml2AssertionValidationOptions()
        {
            Saml2SecurityTokenHandlerFactory = c => new Saml2BearerGrantSecurityTokenHandler(c);
        }

        public Uri Recipient { get; set; }
        public IList<Uri> Audience { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public Func<ISaml2AssertionValidationOptions, Saml2SecurityTokenHandler> Saml2SecurityTokenHandlerFactory { get; set; }
    }
}