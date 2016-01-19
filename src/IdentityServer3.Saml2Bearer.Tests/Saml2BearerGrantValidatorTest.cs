using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IdentityModel.Metadata;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using IdentityServer3.Core.Extensions;
using IdentityServer3.Core.Logging;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Validation;
using Moq;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace IdentityServer3.Saml2Bearer.Tests
{
    [TestFixture]
    public class Saml2BearerGrantValidatorTest
    {
        private Mock<ILog> log;
        Saml2BearerGrantValidator underTest;
        private IUserService userService;
        private ISaml2AssertionFactory factory;
        private ISaml2AssertionSerializer serializer;

        private X509Certificate2 certificate = TestCertificates.Default;
        private X509Certificate2 certificate2 = TestCertificates.Idsrc3Test;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
        }

        [SetUp]
        public void SetUp()
        {
            log = new Mock<ILog>();
            var options = new Saml2AssertionValidationOptions()
            {
                Certificate = certificate,
                Recipient = new Uri("http://allowed_recipient1"),
                Audience = new List<Uri>() { new Uri("http://audience") },
            };
            factory = new Saml2AssertionFactory(options);
            factory.TokenHandler.Configuration.CertificateValidator = X509CertificateValidator.None;
            serializer = new Saml2AssertionSerializer();

            userService = Substitute.For<IUserService>();

            underTest = new Saml2BearerGrantValidator(userService, factory);
            Saml2BearerGrantValidator.Log = log.Object;
        }

        [Test]
        public void Should_Throw_If_Constructor_Arguments_Null()
        {
            // Given, When, Then
            Assert.Throws(Is.TypeOf<ArgumentNullException>().And.Message.Contains("factory"), () => new Saml2BearerGrantValidator(userService, null));
            
            // Given, When, Then
            Assert.Throws(Is.TypeOf<ArgumentNullException>().And.Message.Contains("users"), () => new Saml2BearerGrantValidator(null, factory));
        }

        [Test]
        public void Should_Implement_ICustomGrantValidator()
        {
            // Given, When, Then
            Assert.That(underTest, Is.AssignableTo(typeof(ICustomGrantValidator)));
        }

        [Test]
        public void Should_Return_GrantType()
        {
            // Given, When, Then
            Assert.That(underTest.GrantType, Is.EqualTo("urn:ietf:params:oauth:grant-type:saml2-bearer"));
        }

        [Test]
        public void ValidateAsync_Given_Null_Request_Should_Throw_ArgumentNullException()
        {
            // Given, When, Then
            Assert.Throws(Is.TypeOf<ArgumentNullException>().And.Message.Contains("request"), async () => await underTest.ValidateAsync(null));
        }

        [Test]
        public void ValidateAsync_Given_Missing_Assertion_Should_Throw_ArgumentNullException()
        {
            // Given
            var request = new ValidatedTokenRequest();

            // When, Then
            Assert.Throws(Is.TypeOf<ArgumentNullException>().And.Message.Contains("SAML response not found"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public void ValidateAsync_Given_Malformed_Assertion_Should_Throw_ArgumentNullException()
        {
            // Given
            var request = Request("nothing here");

            // When, Then
            Assert.Throws(Is.TypeOf<SamlAssertionFormatException>()
                .And.Message.Contains("SAML assertion encoding error"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public void ValidateAsync_Given_Malformed_Xml_Assertion_Should_Throw_ArgumentNullException()
        {
            // Given
            var request = Request("nothing here".ToBase64Url());

            // When, Then
            Assert.Throws(Is.TypeOf<SamlAssertionFormatException>()
                .And.Message.Contains("SAML assertion invalid"), async () => await underTest.ValidateAsync(request));
        }

        /// <summary>
        /// https://tools.ietf.org/html/rfc7522#section-3 
        /// </summary>
        [Test]
        public async Task ValidateAsync_3_1_Given_SAML_Without_Valid_Issuer_Throws()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate2);
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<SecurityTokenException>()
                .And.Message.Contains("ID4175"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_2_Given_SAML_With_Emmpty_Conditions_Throws()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Conditions = new Saml2Conditions();
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<AudienceUriValidationFailedException>()
                .And.Message.Contains("ID1035"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_2_Given_SAML_With_Unauthorised_AudienceRestriction_Throws()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Conditions = new Saml2Conditions();
            assertion.Conditions.AudienceRestrictions.Add(new Saml2AudienceRestriction(new Uri("http://unauthorised.example.com")));
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<AudienceUriValidationFailedException>()
                .And.Message.Contains("ID1038"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_2_Given_SAML_Without_Conditions_Throws()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Conditions = null;
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<AudienceUriValidationFailedException>()
                .And.Message.Contains("ID1035"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_4_Given_SAML_Without_NotOnOrAfter_Throws()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Subject.SubjectConfirmations[0].SubjectConfirmationData.NotOnOrAfter = null;
            assertion.Conditions.NotOnOrAfter = null;
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<SecurityTokenExpiredException>()
                .And.Message.Contains("NotOnOrAfter is missing"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_4_Given_SAML_With_NotOnOrAfter_On_Subject_Expired_Throws()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Subject.SubjectConfirmations[0].SubjectConfirmationData.NotOnOrAfter = DateTime.UtcNow.AddHours(-1);
            assertion.Conditions.NotOnOrAfter = null;
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<SecurityTokenExpiredException>()
                .And.Message.Contains("NotOnOrAfter invalid on SubjectConfirmationData"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_4_Given_SAML_With_NotOnOrAfter_On_Conditions_Expired_Throws()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Subject.SubjectConfirmations[0].SubjectConfirmationData.NotOnOrAfter = null;
            assertion.Conditions.NotOnOrAfter = DateTime.UtcNow.AddHours(-1);
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<SecurityTokenExpiredException>()
                .And.Message.Contains("ID4148"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_5_Given_SAML_Without_Bearer_Method_Throw()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Subject.SubjectConfirmations[0].Method = new Uri("http://invalid");
            //assertion.
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<SecurityTokenException>()
                .And.Message.Contains("ID4136"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_5_Given_SAML_With_Unsupported_Method_Throw()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Subject.SubjectConfirmations[0].Method = new Uri("urn:oasis:names:tc:SAML:2.0:cm:holder-of-key");
            //assertion.
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<SecurityTokenException>()
                .And.Message.Contains("ID4134"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_3_5_Given_SAML_With_Wrong_Recipient_Throw()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            assertion.Subject.SubjectConfirmations[0].SubjectConfirmationData.Recipient = new Uri("http://unknown-recipient");
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<SecurityTokenException>()
                .And.Message.Contains("Recipient not valid"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_Given_Valid_SAML_Should_Return_Principal()
        {
            // Given
            var request = Request(GenerateSamlAssertion());

            // When
            var result = await underTest.ValidateAsync(request);

            // Then
            Assert.That(result.ErrorDescription, Is.EqualTo(null));
            Assert.That(result.Error, Is.EqualTo(null));
            Assert.That(result.IsError, Is.EqualTo(false));
            var claims = result.Principal.Claims.ToArray();
            Assert.That(result.Principal.Claims.Count(), Is.EqualTo(9));
            AssertClaim(claims[0], "sub", "id3");
            AssertClaim(claims[1], "amr", "custom");
            AssertClaim(claims[2], "idp", "idsrv");
            var auth_time = DateTimeOffset.UtcNow.ToUnixTime();
            AssertClaim(claims[3], "auth_time");
            Assert.That(long.Parse(claims[3].Value), Is.LessThanOrEqualTo(auth_time));
            AssertClaim(claims[4], "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "id3");
            AssertClaim(claims[5], "identifiantFacturation", "id1");
            AssertClaim(claims[6], "codeSpecialiteAMO", "id2");
            AssertClaim(claims[7], "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "RoleFromClaimsAuthManager");
            AssertClaim(claims[8], "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "John_ID");
            Assert.That(result.Principal.GetSubjectId(), Is.EqualTo("id3"));
        }

        private void AssertClaim(Claim claim, string name1)
        {
            Assert.That(claim.Type, Is.EqualTo(name1));
        }

        private void AssertClaim(Claim claim, string name1, object val1)
        {
            Assert.That(claim.Type, Is.EqualTo(name1));
            Assert.That(claim.Value, Is.EqualTo(val1));
        }

        [Test]
        public async Task ValidateAsync_Given_Custom_Subject_Selector_Should_Return_Principal()
        {
            // Given
            var claimsIdentity = GetClaimsIdentity();
            var sub = Guid.NewGuid().ToString();
            claimsIdentity.AddClaim(new Claim("sub", sub));
            var request = Request(GenerateSamlAssertion(id:claimsIdentity));
            underTest.SubjectSelector = c => c.Single(y => y.Type == "sub");

            // When
            var result = await underTest.ValidateAsync(request);

            // Then
            Assert.That(result.IsError, Is.EqualTo(false));
            Assert.That(result.Principal.GetSubjectId(), Is.EqualTo(sub));
        }

        [Test]
        public async Task ValidateAsync_Given_Custom_Subject_Selector_Throws_Should_Return_Principal()
        {
            // Given
            var request = Request(GenerateSamlAssertion());
            underTest.SubjectSelector = c => c.Single(y => y.Type == "unknownClaim");

            // When
            var result = await underTest.ValidateAsync(request);

            // Then
            Assert.That(result.IsError, Is.EqualTo(true));
            HasLogMessageException(Is.EqualTo("Action=ValidateAsync, Message=Error in subject selector func"));
            HasLogMessage(Is.EqualTo("Action=ValidateAsync, Message=Subject in SAML assertion is not present"));
        }

        private void HasLogMessageException(EqualConstraint s)
        {
            // NSubstitute 
            // log.Received().Log(LogLevel.Error, Arg.Is<Func<string>>(x => Verify(x, s)), exception);
            log.Verify(y => y.Log(LogLevel.Error, It.Is<Func<string>>(x => Verify(x, s)), It.IsAny<Exception>()));
        }

        private void HasLogMessage(EqualConstraint s)
        {
            // NSubstitute 
            //log.Received().Log(LogLevel.Error, Arg.Is<Func<string>>(x => Verify(x, s)));
            log.Verify(y=>y.Log(LogLevel.Error, It.Is<Func<string>>(x => Verify(x, s)), null));
        }

        private bool Verify(Func<string> s, Constraint equalConstraint)
        {
            var str = s();
            var eq = equalConstraint.Matches(str);
            return eq;
        }

        private Saml2Assertion GenerateSamlAssertion(X509Certificate2 x509Certificate2 = null, ClaimsIdentity id = null)
        {
            id = id ?? GetClaimsIdentity();

            var assertion = NewAssertion(x509Certificate2, id);
            return assertion;
            //return new Saml2Response(spOptions.EntityId, certificate, new Uri("http://localhost:2020"), null, id);
        }

        private Saml2Assertion NewAssertion(X509Certificate2 x509Certificate2 = null, ClaimsIdentity id = null)
        {
            var cert = x509Certificate2 ?? certificate;
            var assertion = new Saml2Assertion(new Saml2NameIdentifier("b"));
            assertion.Id = new Saml2Id();
            assertion.IssueInstant = DateTime.Now;

            assertion.Subject = new Saml2Subject(new Saml2NameIdentifier("CPS") { Value = "id3" });
            assertion.Statements.Add(new Saml2AttributeStatement(new Saml2Attribute("identifiantFacturation", "id1")));
            assertion.Statements.Add(new Saml2AttributeStatement(new Saml2Attribute("codeSpecialiteAMO", "id2")));
            if (id != null)
            foreach (var claim in id.Claims)
            {
                assertion.Statements.Add(new Saml2AttributeStatement(new Saml2Attribute(claim.Type, claim.Value)));
            }


            var saml2SubjectConfirmation = new Saml2SubjectConfirmation(new Uri("urn:oasis:names:tc:SAML:2.0:cm:bearer"));
            saml2SubjectConfirmation.SubjectConfirmationData = new Saml2SubjectConfirmationData();
            saml2SubjectConfirmation.SubjectConfirmationData.Recipient = new Uri("http://allowed_recipient1");
            saml2SubjectConfirmation.SubjectConfirmationData.NotOnOrAfter = DateTime.UtcNow.AddHours(1); // valid for 1 hour

            assertion.Subject.SubjectConfirmations.Add(saml2SubjectConfirmation);

            assertion.Conditions = new Saml2Conditions();
            assertion.Conditions.AudienceRestrictions.Add(new Saml2AudienceRestriction(new Uri("http://audience")));

            assertion.SigningCredentials = new X509SigningCredentials(cert);
            return assertion;
        }

        private static ClaimsIdentity GetClaimsIdentity()
        {
            var id = new ClaimsIdentity("ClaimsAuthenticationManager");
            id.AddClaim(new Claim(ClaimTypes.Role, "RoleFromClaimsAuthManager", null, "ClaimsAuthenticationManagerStub"));
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, "John_ID", null, "ClaimsAuthenticationManagerStub"));
            return id;
        }

        private ValidatedTokenRequest Request(Saml2Assertion saml)
        {
            var serialized = serializer.ToXml(saml);
            //Console.WriteLine(serialized);
            return Request(serialized.ToBase64Url());
        }

        private static ValidatedTokenRequest Request(string assertion)
        {
            var request = new ValidatedTokenRequest()
            {
                Raw = new NameValueCollection()
            };
            request.Raw["assertion"] = assertion;
            return request;
        }
    }
}
