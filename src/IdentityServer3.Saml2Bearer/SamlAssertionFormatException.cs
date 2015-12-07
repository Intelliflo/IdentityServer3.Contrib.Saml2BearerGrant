using System;

namespace IdentityServer3.Saml2Bearer
{
    public class SamlAssertionFormatException : Exception
    {
        public SamlAssertionFormatException(string message, Exception e) : base(message, e) { }
    }
}