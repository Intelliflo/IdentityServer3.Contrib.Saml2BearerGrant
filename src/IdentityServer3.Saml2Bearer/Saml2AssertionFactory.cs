using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace IdentityServer3.Saml2Bearer
{
    public class Saml2AssertionFactory : ISaml2AssertionFactory
    {
        Saml2SecurityTokenHandler tokenHandler;
        private SecurityTokenHandlerConfiguration configuration;

        public Saml2AssertionFactory(ISaml2AssertionValidationOptions options)
        {
            if (options.Audience == null)
                throw new ArgumentNullException("Audience");
            if (options.Recipient == null)
                throw new ArgumentNullException("Recipient");
            if (options.Certificate == null)
                throw new ArgumentNullException("certificate");
            configuration = GetSecurityTokenHandlerConfiguration(options);
            tokenHandler = options.Saml2SecurityTokenHandlerFactory(options);
            tokenHandler.Configuration = configuration;
        }

        public Saml2SecurityTokenHandler TokenHandler
        {
            get { return tokenHandler; }
            set { tokenHandler = value; }
        }

        public Saml2SecurityToken ToSecurityToken(string xml)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var tokenReader = new XmlNodeReader(xmlDoc); // XML document with root element <saml:EncryptedAssertion ....
            if (!tokenHandler.CanReadToken(tokenReader))
                throw new Exception("Unreadable token");

            var token = tokenHandler.ReadToken(tokenReader);
            return token as Saml2SecurityToken;
        }

        protected virtual SecurityTokenHandlerConfiguration GetSecurityTokenHandlerConfiguration(ISaml2AssertionValidationOptions options)
        {
            var serviceTokens = new List<SecurityToken>();
            serviceTokens.Add(new X509SecurityToken(options.Certificate));

            var issuers = new ConfigurationBasedIssuerNameRegistry();
            issuers.AddTrustedIssuer(options.Certificate.Thumbprint, options.Certificate.Issuer);

            var conf = new SecurityTokenHandlerConfiguration
            {
                AudienceRestriction = new AudienceRestriction(AudienceUriMode.Always),
                CertificateValidator = X509CertificateValidator.ChainTrust,
                RevocationMode = X509RevocationMode.NoCheck,
                IssuerNameRegistry = issuers,
                MaxClockSkew = TimeSpan.FromMinutes(5),
                ServiceTokenResolver =
                    SecurityTokenResolver.CreateDefaultSecurityTokenResolver(serviceTokens.AsReadOnly(), false)
            };
            foreach (var y in options.Audience)
            {
                conf.AudienceRestriction.AllowedAudienceUris.Add(y);
            }
            return conf;
        }
    }
}