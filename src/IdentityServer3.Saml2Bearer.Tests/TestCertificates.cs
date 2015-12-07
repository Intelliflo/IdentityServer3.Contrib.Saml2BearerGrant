using System.Security.Cryptography.X509Certificates;

namespace IdentityServer3.Saml2Bearer.Tests
{
    internal class TestCertificates
    {
        public static X509Certificate2 KentorAuthServices { get { return new X509Certificate2("certs/Kentor.AuthServices.Tests.pfx"); } }
        public static X509Certificate2 Idsrv3Test { get { return new X509Certificate2("certs/idsrv3test.pfx", "idsrv3test", X509KeyStorageFlags.Exportable); } }
    }
}