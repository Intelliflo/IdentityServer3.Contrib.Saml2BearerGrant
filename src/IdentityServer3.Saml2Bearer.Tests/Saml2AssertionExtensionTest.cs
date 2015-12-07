using System;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using ApprovalTests;
using NUnit.Framework;

namespace IdentityServer3.Saml2Bearer.Tests
{
    [TestFixture]
    public class Saml2AssertionExtensionTest
    {
        private Saml2AssertionFactory factory;
        X509Certificate2 certificate = TestCertificates.Idsrv3Test;
        static string Generated = "yyyy-MM-ddTHH:mm:ss.fffZ";

        [SetUp]
        public void SetUp()
        {
            factory = new Saml2AssertionFactory(
                new Saml2AssertionValidationOptions()
                {
                    Certificate = certificate,
                }
                );
        }

        
    }
}