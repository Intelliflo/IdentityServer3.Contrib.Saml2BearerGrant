using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;

namespace IdentityServer3.Contrib.Saml2Bearer.Tests
{
    [TestFixture]
    public class Saml2AssertionFactoryTest
    {
        private Saml2AssertionFactory underTest;
        private Saml2AssertionSerializer serializer;
        X509Certificate2 certificate = TestCertificates.Default;
        static string Generated = "yyyy-MM-ddTHH:mm:ss.fffZ";

        [SetUp]
        public void SetUp()
        {
            serializer = new Saml2AssertionSerializer();
            underTest = new Saml2AssertionFactory(
                new Saml2AssertionValidationOptions()
                {
                    Certificate = certificate,
                    Audience = new List<Uri>() { new Uri( "http://audience.com") },
                    Recipient = new Uri("http://recipient.com")
                });
        }

        [Test]
        public void Serialize()
        {
            // Given
            var assertion = NewAssertion();

            // When
            var serialized = serializer.ToXml(assertion);

            // Then
            var expected = GetExpected(assertion);
            Assert.That(expected, Is.Not.Empty);
            Assert.That(serialized.Substring(0, expected.Length), Is.EqualTo(expected));
        }

        private string GetExpected(Saml2Assertion assertion)
        {
            return string.Format(@"
<Assertion ID=""{0}"" IssueInstant=""{1}"" Version=""2.0"" xmlns=""urn:oasis:names:tc:SAML:2.0:assertion"">
    <Issuer>{2}</Issuer>
    <Signature xmlns=""http://www.w3.org/2000/09/xmldsig#"">
    <SignedInfo>
    <CanonicalizationMethod Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
    <SignatureMethod Algorithm=""http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"" />
    <Reference URI=""#{0}"">
    <Transforms>
    <Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" />
        <Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
        </Transforms>
        <DigestMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#sha256"" />
",
                assertion.Id.Value, assertion.IssueInstant.ToUniversalTime().ToString(Generated),
                certificate.IssuerName.Name
                )
                .RemoveNewLineSymbols()
                .RemoveXmlIndentation();
        }

        [Test]
        public void Deserialise()
        {
            // Given
            var givenAssertion = NewAssertion();
            var serializedAssertion = serializer.ToXml(givenAssertion);

            // When
            var assertion = underTest.ToSecurityToken(serializedAssertion).Assertion;

            // Then
            Assert.That(assertion, Is.Not.Null);
            Assert.That(assertion.Id, Is.EqualTo(givenAssertion.Id));
        }

        private Saml2Assertion NewAssertion()
        {
            var assertion = new Saml2Assertion(new Saml2NameIdentifier(certificate.Subject));
            assertion.Id = new Saml2Id();
            assertion.IssueInstant = DateTime.Now;

            assertion.Subject = new Saml2Subject(new Saml2NameIdentifier("CPS") { Value = "id3" });
            assertion.Statements.Add(new Saml2AttributeStatement(new Saml2Attribute("identifiantFacturation", "id1")));
            assertion.Statements.Add(new Saml2AttributeStatement(new Saml2Attribute("codeSpecialiteAMO", "id2")));

            var saml2SubjectConfirmation = new Saml2SubjectConfirmation(new Uri("urn:oasis:names:tc:SAML:2.0:cm:bearer"));
            saml2SubjectConfirmation.SubjectConfirmationData = new Saml2SubjectConfirmationData();
            saml2SubjectConfirmation.SubjectConfirmationData.Recipient = new Uri("http://identityserver3");
            saml2SubjectConfirmation.SubjectConfirmationData.NotOnOrAfter = DateTime.UtcNow;

            assertion.Subject.SubjectConfirmations.Add(saml2SubjectConfirmation);

            assertion.SigningCredentials = new X509SigningCredentials(certificate);
            return assertion;
        }

    }
}