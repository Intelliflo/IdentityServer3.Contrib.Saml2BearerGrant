using System;
using NUnit.Framework;

namespace IdentityServer3.Saml2Bearer.Tests
{
    [TestFixture]
    public class Base64UrlExtensionsTest
    {
        [TestCase("a", "YQ")] // basic string handling
        [TestCase("", "")] // empty string handling
        [TestCase("123", "MTIz")] // byte array length equal to 4
        [TestCase("12345678901234567890", "MTIzNDU2Nzg5MDEyMzQ1Njc4OTA")] // longer string handling
        [Test]
        public void Convert(string str, string expected)
        {
            // Given, When, Then
            Assert.That(str.ToBase64Url(), Is.EqualTo(expected) );
            Assert.That(str.ToBase64Url().FromBase64Url(), Is.EqualTo(str));

            // Given, When, Then
            Assert.That(expected.FromBase64Url(), Is.EqualTo(str));
            Assert.That(expected.FromBase64Url().ToBase64Url(), Is.EqualTo(expected));
        }

        [TestCase(null)]
        public void NullHandling(string str)
        {
            // Given, When, Then
            Assert.Throws(Is.TypeOf<ArgumentNullException>()
                .And.Message.Contains("str"), () => str.ToBase64Url());
        }
    }
}