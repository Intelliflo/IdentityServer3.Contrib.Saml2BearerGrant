using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Validation;
using Kentor.AuthServices;
using Kentor.AuthServices.Configuration;
using Kentor.AuthServices.Metadata;
using Kentor.AuthServices.Saml2P;
using Kentor.AuthServices.WebSso;
using NSubstitute;
using NUnit.Framework;

namespace IdentityServer3.Saml2Bearer.Tests
{
    [TestFixture]
    public class Saml2BearerGrantValidatorTest
    {
        Saml2BearerGrantValidator underTest;
        private IUserService userService;

        private string ValidSaml = "<?xml version=\"1.0\"?><saml2p:Response Destination=\"http://localhost:57294/AuthServices/Acs\" ID=\"id54d3c8f2b69c44f59edcae890557e25e\" Version=\"2.0\" IssueInstant=\"2015-11-12T13:29:53Z\" xmlns:saml2p=\"urn:oasis:names:tc:SAML:2.0:protocol\"><saml2:Issuer xmlns:saml2=\"urn:oasis:names:tc:SAML:2.0:assertion\">http://localhost:52071/Metadata</saml2:Issuer><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#id54d3c8f2b69c44f59edcae890557e25e\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>Rtni9oUwavU07yryQOyXKmYblq8=</DigestValue></Reference></SignedInfo><SignatureValue>irvtwxSg+SrEj5dSjpDpC6VXv3dU4KGEnmdUCffZ0ooaAFIPH1op1dwzgIRZxKYk+lk/zt3IJ1LSC7qw6RWY4G3GR4Akp5HtCF2Js45Y7CQFWQ251+uf/hNOgdzyYfgoQQgGigrbJMsMALf707GI4sjrjY7JHF6rASMvs9nw3g5oo9mHFyRkonQZxzT23D9UyfmVK1x93yW8mSXgVQvkUD8tx1lTAYIupPJvlIfYxrUaG4hViYK1bWRXVf6JR8OpcLKmLssepq/q+LFzBoxDEVlwHRwXQ7XNAJzKEPNxXrinAQox7dKST8UJ0nnz4+5HRxue+vWcwNcfoq3hpZjIew==</SignatureValue></Signature><saml2p:Status><saml2p:StatusCode Value=\"urn:oasis:names:tc:SAML:2.0:status:Success\" /></saml2p:Status><saml2:Assertion xmlns:saml2=\"urn:oasis:names:tc:SAML:2.0:assertion\" Version=\"2.0\" ID=\"_5e4f31d9-fb31-4d31-8e5b-59abf733aea5\" IssueInstant=\"2015-11-12T13:29:53Z\"><saml2:Issuer>http://localhost:52071/Metadata</saml2:Issuer><saml2:Subject><saml2:NameID>JohnDoe</saml2:NameID><saml2:SubjectConfirmation Method=\"urn:oasis:names:tc:SAML:2.0:cm:bearer\" /></saml2:Subject><saml2:Conditions NotOnOrAfter=\"2015-11-12T13:31:53Z\" /></saml2:Assertion></saml2p:Response>";
        private IOptions options;
        private SPOptions spOptions;
        X509Certificate2 certificate = CertificateHelper.Load();

        [SetUp]
        public void SetUp()
        {
            spOptions = CreateSPOptions();
            options = new Options(spOptions);
            //Substitute.For<IOptions>();

            options.IdentityProviders.Add(Idp());
            //options.IdentityProviders.Returns(ReturnThis());


            userService = Substitute.For<IUserService>();
            underTest = new Saml2BearerGrantValidator(userService, options);
        }

        private IdentityProviderDictionary ReturnThis()
        {
            var p = new IdentityProviderDictionary();
            p.Add(Idp());
            return p;
        }

        private IdentityProvider Idp()
        {
            //            var ipd = new IdentityProvider(new EntityId("http://localhost:52071/Metadata"), null);
            var spOptions = CreateSPOptions();
            var idp = new IdentityProvider(new EntityId("http://localhost:52071/Metadata"), spOptions)
            {
                AllowUnsolicitedAuthnResponse = true,
                Binding = Saml2BindingType.HttpRedirect,
                SingleSignOnServiceUrl = new Uri("http://stubidp.kentor.se")
            };

            idp.SigningKeys.AddConfiguredItem(certificate.PublicKey.Key);
            return idp;
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

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        [Test]
        public void ValidateAsync_Given_Malformed_Xml_Assertion_Should_Throw_ArgumentNullException()
        {
            // Given
            var request = Request(Base64Encode("nothing here"));

            // When, Then
            Assert.Throws(Is.TypeOf<SamlAssertionFormatException>()
                .And.Message.Contains("SAML assertion invalid"), async () => await underTest.ValidateAsync(request));
        }

        [Test]
        public async Task ValidateAsync_Given_Valid_SAML_Should_Return_Principal()
        {
            Options.GlobalEnableSha256XmlSignatures();
            // Given
            var saml = GenerateSaml();
            var encodedXml = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(saml.ToXml()));
            var request = Request(encodedXml);

            // When
            var result = await underTest.ValidateAsync(request);

            // Then
            Assert.That(result.IsError, Is.EqualTo(false));
            Assert.That(result.Principal.Claims.Count(), Is.EqualTo(6));
        }

        private Saml2Response GenerateSaml()
        {

            var id = new ClaimsIdentity("ClaimsAuthenticationManager");
            id.AddClaim(new Claim(ClaimTypes.Role, "RoleFromClaimsAuthManager", null, "ClaimsAuthenticationManagerStub"));
            id.AddClaim(new Claim(ClaimTypes.NameIdentifier, "John_ID", null, "ClaimsAuthenticationManagerStub"));
            

            return new Saml2Response(spOptions.EntityId, certificate, new Uri("http://localhost:2020"),
                null, id);

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

        private static SPOptions CreateSPOptions()
        {
            var swedish = CultureInfo.GetCultureInfo("sv-se");

            var organization = new Organization();
            organization.Names.Add(new LocalizedName("Kentor", swedish));
            organization.DisplayNames.Add(new LocalizedName("Kentor IT AB", swedish));
            organization.Urls.Add(new LocalizedUri(new Uri("http://www.kentor.se"), swedish));

            var spOptions = new SPOptions
            {
                EntityId = new EntityId("http://localhost:52071/Metadata"),
                ReturnUrl = new Uri("http://localhost:57294/Account/ExternalLoginCallback"),
                DiscoveryServiceUrl = new Uri("http://localhost:52071/DiscoveryService"),
                Organization = organization
            };

            var techContact = new ContactPerson
            {
                Type = ContactType.Technical
            };
            techContact.EmailAddresses.Add("authservices@example.com");
            spOptions.Contacts.Add(techContact);

            var supportContact = new ContactPerson
            {
                Type = ContactType.Support
            };
            supportContact.EmailAddresses.Add("support@example.com");
            spOptions.Contacts.Add(supportContact);

            var attributeConsumingService = new AttributeConsumingService("AuthServices")
            {
                IsDefault = true,
            };

            attributeConsumingService.RequestedAttributes.Add(
                new RequestedAttribute("urn:someName")
                {
                    FriendlyName = "Some Name",
                    IsRequired = true,
                    NameFormat = RequestedAttribute.AttributeNameFormatUri
                });

            attributeConsumingService.RequestedAttributes.Add(
                new RequestedAttribute("Minimal"));

            spOptions.AttributeConsumingServices.Add(attributeConsumingService);

            return spOptions;
        }
    }
}
