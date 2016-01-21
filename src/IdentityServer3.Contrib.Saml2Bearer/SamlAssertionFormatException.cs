using System;

namespace IdentityServer3.Contrib.Saml2Bearer
{
    public class SamlAssertionFormatException : Exception
    {
        public SamlAssertionFormatException(string message, Exception e) : base(message, e) { }
    }
}