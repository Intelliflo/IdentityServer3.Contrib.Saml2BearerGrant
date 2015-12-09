using System.IdentityModel.Tokens;

namespace IdentityServer3.Saml2Bearer
{
    public interface ISaml2AssertionFactory
    {
        Saml2SecurityToken ToSecurityToken(string xml);
        Saml2SecurityTokenHandler TokenHandler { get; }
    }
}