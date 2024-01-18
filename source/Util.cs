using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Maestro
{
    internal class Util
    {
        public static string GetMatch(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            Logger.Debug($"No matching string was found for: {pattern}");
            return null;
        }
    }
}