using System;
using System.Text.RegularExpressions;

namespace Maestro
{
    internal class Util
    {
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

        public static string TruncateString(string str)
        {
            if (str.Length <= 12)
                return str;
            return str.Substring(0, 6) + "..." + str.Substring(str.Length - 6);
        }
    }
}