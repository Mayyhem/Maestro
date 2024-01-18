using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Maestro
{
    internal class Util
    {
        public static T ExecuteAndCheckForNull<T>(Func<T> function, string functionName)
        {
            var result = function();
            if (result == null)
            {
                Logger.Error($"{functionName} returned a null value");
            }
            return result;
        }

        public static async Task<T> ExecuteAndCheckForNullAsync<T>(Func<Task<T>> function, string functionName)
        {
            var result = await function();
            if (result == null)
            {
                Logger.Error($"{functionName} returned a null value");
            }
            return result;
        }

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