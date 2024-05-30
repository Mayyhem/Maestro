using System.Text;
using System;
using System.Text.RegularExpressions;

namespace Maestro
{
    internal class StringHandler
    {
        public static string DecodeJwt(string jwt)
        {
            // Split the JWT into its parts
            string[] parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("The JWT is not in a valid format.");
            }

            string base64 = parts[1].Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var base64EncodedBytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string GetMatch(string input, string pattern, bool group = true)
        {
            var match = Regex.Match(input, pattern);
            if (!match.Success)
            {
                Logger.Debug($"No matching string was found for: {pattern}");
                return null;
            }

            if (group)
            {
                return match.Groups[1].Value;
            }

            return match.Value;
        }

        public static string Truncate(string str)
        {
            if (str.Length <= 12)
                return str;
            return str.Substring(0, 6) + "..." + str.Substring(str.Length - 6);
        }
    }
}