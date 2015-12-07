
using System;
using System.Text;
using System.Web;

namespace IdentityServer3.Saml2Bearer
{
    public static class Base64UrlExtensions
    {
        /// <summary>
        /// Encodes string to base64url, as specified by rfc4648 (https://tools.ietf.org/html/rfc4648#section-5)
        /// </summary>
        /// <returns></returns>
        public static string ToBase64Url(this string str)
        {
            // TODO - adds reference to System.Web
            if (str == null)
                throw new ArgumentNullException("str");
            var customBase64 = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(str));
            return customBase64.Length == 0 ? customBase64 : customBase64.Substring(0, customBase64.Length - 1);
        }

        public static string FromBase64Url(this string rfc4648)
        {
            if (rfc4648.Length%4 != 0)
                rfc4648 += (4 - rfc4648.Length%4);
            else
                rfc4648 += 0;

            return Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(rfc4648));
        }
    }
}