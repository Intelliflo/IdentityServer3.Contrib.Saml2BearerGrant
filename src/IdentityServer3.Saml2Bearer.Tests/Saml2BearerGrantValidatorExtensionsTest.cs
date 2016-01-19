using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using IdentityServer3.Core.Logging;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Validation;
using Moq;
using NSubstitute;
using NUnit.Framework;

namespace IdentityServer3.Saml2Bearer.Tests
{
    [TestFixture]
    public class Saml2BearerGrantValidatorExtensionsTest
    {
        private static string customValidationError = "Custom validation error 01";
        public class ThrowExceptionSamlSecurityTokenRequirement : SamlSecurityTokenRequirement
        {
            public override void ValidateAudienceRestriction(IList<Uri> allowedAudienceUris, IList<Uri> tokenAudiences)
            {
                throw new Exception(customValidationError);
            }
        }

        private Mock<ILog> log;
        Saml2BearerGrantValidator underTest;
        private IUserService userService;
        private ISaml2AssertionFactory factory;
        private ISaml2AssertionSerializer serializer;

        private X509Certificate2 certificate = TestCertificates.Default;

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
                Saml2SecurityTokenHandlerFactory = c => new Saml2BearerGrantSecurityTokenHandler(c, new ThrowExceptionSamlSecurityTokenRequirement()) 
            };
            factory = new Saml2AssertionFactory(options);
            factory.TokenHandler.Configuration.CertificateValidator = X509CertificateValidator.None;
            serializer = new Saml2AssertionSerializer();

            userService = Substitute.For<IUserService>();

            underTest = new Saml2BearerGrantValidator(userService, factory);
            Saml2BearerGrantValidator.Log = log.Object;
        }

        [Test]
        public async Task ValidateAsync_AllowTo_Pass_Custom_Audience_Validator()
        {
            // Given
            var assertion = GenerateSamlAssertion(certificate);
            var request = Request(assertion);

            // When, Then
            Assert.Throws(Is.TypeOf<Exception>()
               .And.Message.Contains(customValidationError), async () => await underTest.ValidateAsync(request));
        }

        private ValidatedTokenRequest Request(Saml2Assertion saml)
        {
            var serialized = serializer.ToXml(saml);
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
    }
}