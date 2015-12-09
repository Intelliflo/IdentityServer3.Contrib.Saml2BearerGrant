using System.Security.Cryptography.X509Certificates;

namespace IdentityServer3.Saml2Bearer.Tests
{
    internal class TestCertificates
    {
        public static X509Certificate2 Default { get { return new X509Certificate2("certs/IdentityServer3.Saml2Bearer.pfx", "password"); } }
        public static X509Certificate2 Idsrc3Test { get { return new X509Certificate2("certs/idsrv3test.pfx", "idsrv3test"); } }
    }
}