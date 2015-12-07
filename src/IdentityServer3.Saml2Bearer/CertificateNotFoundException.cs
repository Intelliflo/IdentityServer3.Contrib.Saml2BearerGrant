using System;

namespace IdentityServer3.Saml2Bearer
{
    public class CertificateNotFoundException : Exception
    {
        public CertificateNotFoundException (string message) : base(message) { }
    }
}