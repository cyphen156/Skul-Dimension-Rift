using System;
using System.Globalization;

namespace Assets.Scripts.Data
{
    public static class DomainKeyParser
    {
        public static bool TryParseStaticKey(string text, out uint value)
        {
            value = 0u;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string s = text.Trim();

            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(2);
                return uint.TryParse(
                    s,
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out value
                );
            }

            return uint.TryParse(
                s,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        public static string ToHex(uint key)
        {
            return "0x" + key.ToString("X8");
        }
    }
}
