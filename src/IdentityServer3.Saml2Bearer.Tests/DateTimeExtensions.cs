using System;

namespace IdentityServer3.Saml2Bearer.Tests
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

        public static long ToUnixTime(this DateTimeOffset date)
        {
            var epoch = new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }
    }
}