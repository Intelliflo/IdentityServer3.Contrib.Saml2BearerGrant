using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using IdentityServer3.Core.Logging;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Validation;

namespace IdentityServer3.Contrib.Saml2Bearer
{
    public class Saml2BearerGrantValidator : ICustomGrantValidator
    {
        private static ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly IUserService _users;
        private readonly ISaml2AssertionFactory _factory;
        private readonly string nameClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        public Saml2BearerGrantValidator(IUserService users, ISaml2AssertionFactory factory)
        {
            if (users == null)
                throw new ArgumentNullException("users");
            if (factory == null)
                throw new ArgumentNullException("factory");
            this._users = users;
            _factory = factory;
            SubjectSelector = c => c.First(y => y.Type == nameClaim);
        }

        public static ILog Log
        {
            set { Logger = value; }
        }

        public Func<IEnumerable<Claim>, Claim> SubjectSelector { get; set; }

        public async Task<CustomGrantValidationResult> ValidateAsync(ValidatedTokenRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var samlResponse = LoadSamlAssertion(request);

            var claimsIdentities =_factory.TokenHandler.ValidateToken(samlResponse);

            var principal = new ClaimsPrincipal(claimsIdentities);
            var subject = GetSubject(principal);
            if (subject == null)
            {
                Logger.Log(LogLevel.Error, () => "Action=ValidateAsync, Message=Subject in SAML assertion is not present");
                return new CustomGrantValidationResult("Subject claim not found in SAML2Bearer assertion");
            }

            return new CustomGrantValidationResult(subject.Value,
                "custom", principal.Claims);
        }

        private Claim GetSubject(ClaimsPrincipal principal)
        {
            try
            {
                return SubjectSelector(principal.Claims);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, () => "Action=ValidateAsync, Message=Error in subject selector func", e);
                return null;
            }
        }

        private Saml2SecurityToken LoadSamlAssertion(ValidatedTokenRequest request)
        {
            var assertion = request.Raw == null ? string.Empty : request.Raw.Get("assertion");
            if (string.IsNullOrWhiteSpace(assertion))
                throw new ArgumentNullException("SAML response not found in assertion query string param");

            var xml = string.Empty;
            try
            {
                xml = assertion.FromBase64Url();
                return _factory.ToSecurityToken(xml);
            }
            catch (FormatException e)
            {
                Logger.ErrorFormat("SAML Response not encoded properly, {0}", assertion);
                throw new SamlAssertionFormatException("SAML assertion encoding error", e);
            }
            catch (XmlException e)
            {
                Logger.ErrorFormat("SAML assertion invalid, {0}", xml);
                throw new SamlAssertionFormatException("SAML assertion invalid", e);
            }
        }

        public string GrantType
        {
            get { return "urn:ietf:params:oauth:grant-type:saml2-bearer"; }
        }
    }
}
