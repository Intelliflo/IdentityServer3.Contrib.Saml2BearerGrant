using System;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens;
using System.Security.Claims;

namespace IdentityServer3.Saml2Bearer
{
    public class Saml2BearerGrantSecurityTokenHandler : Saml2SecurityTokenHandler
    {
        private Uri allowedRecipient;

        public Saml2BearerGrantSecurityTokenHandler(Uri allowedRecipient)
        {
            this.allowedRecipient = allowedRecipient;
        }

        static class ConfirmationMethods
        {
            public static readonly Uri Bearer = new Uri("urn:oasis:names:tc:SAML:2.0:cm:bearer");
        }

        protected override void ValidateConfirmationData(Saml2SubjectConfirmationData confirmationData)
        {
            //confirmationData.Recipient.AbsolutePath 
            //base.ValidateConfirmationData(confirmationData);
        }

        public override ReadOnlyCollection<ClaimsIdentity> ValidateToken(SecurityToken token)
        {
            Saml2SecurityToken samlToken = token as Saml2SecurityToken;
            ValidateMethod(samlToken.Assertion);
            var claimsIdentities = base.ValidateToken(token);
            ValidateAssertion(samlToken.Assertion);
            return claimsIdentities;
        }

        private void ValidateAssertion(Saml2Assertion assertion)
        {
            ValidateNotOnOrAfter(assertion);
            ValidateRecipient(assertion);
        }

        private void ValidateRecipient(Saml2Assertion assertion)
        {
            var actual = assertion.Subject.SubjectConfirmations[0].SubjectConfirmationData.Recipient;
            if (actual != allowedRecipient)
                throw new SecurityTokenException(string.Format("Message=Recipient not valid, Expected={0}, Actual={1}",
                    allowedRecipient, actual));
        }

        private void ValidateMethod(Saml2Assertion assertion)
        {
            // TODO - not covereted, see ValidateAsync_3_5_Given_SAML_With_Unsupported_Method_Throw unit test
            var method = assertion.Subject.SubjectConfirmations[0].Method;
            if (method != ConfirmationMethods.Bearer)
                throw new SecurityTokenException(string.Format("Message=Unsupported method, Expected={0}, Actual={1}",
                    ConfirmationMethods.Bearer, method));
        }

        private void ValidateNotOnOrAfter(Saml2Assertion assertion)
        {
            DateTime now = DateTime.UtcNow;
            /*
            if (assertion.Conditions!=null && assertion.Conditions.NotOnOrAfter != null)
            {
                var conditionValue = assertion.Conditions.NotOnOrAfter + Configuration.MaxClockSkew.Negate();
                if (conditionValue < now)
                    throw new SecurityTokenExpiredException(string.Format("Message=NotOnOrAfter invalid on Conditions, NotOnOrAfter={0}, now={1}", conditionValue, now));
                return;
            }
            */
            if (assertion.Subject.SubjectConfirmations[0].SubjectConfirmationData.NotOnOrAfter == null)
                throw new SecurityTokenExpiredException(string.Format("Message=NotOnOrAfter is missing"));

            var subjectNotOnOrAfter = assertion.Subject.SubjectConfirmations[0].SubjectConfirmationData.NotOnOrAfter + Configuration.MaxClockSkew.Negate();
            if (subjectNotOnOrAfter < now)
                throw new SecurityTokenExpiredException(string.Format("Message=NotOnOrAfter invalid on SubjectConfirmationData, NotOnOrAfter={0}, now={1}", subjectNotOnOrAfter, now));
        }
    }
}
