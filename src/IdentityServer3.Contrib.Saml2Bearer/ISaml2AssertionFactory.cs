using System.IdentityModel.Tokens;

namespace IdentityServer3.Contrib.Saml2Bearer
{
    public interface ISaml2AssertionFactory
    {
        Saml2SecurityToken ToSecurityToken(string xml);
        Saml2SecurityTokenHandler TokenHandler { get; }
    }
}