using System;

namespace IdentityServer3.Saml2Bearer.Tests
{
    public static class StringExtensions
    {
        public static string RemoveNewLineSymbols(this string source)
        {
            return source.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
        }

        public static string RemoveXmlIndentation(this string source)
        {
            return source.Replace("  ", string.Empty);
        }
    }
}