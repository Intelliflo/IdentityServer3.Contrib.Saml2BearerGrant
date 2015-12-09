using System.IdentityModel.Tokens;

namespace IdentityServer3.Saml2Bearer
{
    public interface ISaml2AssertionSerializer
    {
        string ToXml(Saml2Assertion assertion);
    }
}