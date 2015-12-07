using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace IdentityServer3.Saml2Bearer
{
    public interface ISaml2AssertionValidationOptions
    {
        /// <summary>
        /// Indicates the token endpoint URL of the authorization server(or an acceptable alias)
        /// </summary>
        Uri Recipient { get; set; }

        /// <summary>
        /// Identifies the authorization server as an intended audience,
        /// An identifier for a SAML Service Provider with which the authorization server identifies itself.
        /// The authorization server MUST reject any Assertion that does not contain its own identity as the intended audience.
        /// 
        /// TODO: the allowed origins of the token
        ///  </summary>
        IList<Uri> Audience { get; set; }

        /// <summary>
        /// Certificate to verify token signature
        /// </summary>
        X509Certificate2 Certificate { get; set; }
    }

    public class Saml2AssertionValidationOptions : ISaml2AssertionValidationOptions
    {
        public Uri Recipient { get; set; }
        public IList<Uri> Audience { get; set; }
        public X509Certificate2 Certificate { get; set; }
    }
}