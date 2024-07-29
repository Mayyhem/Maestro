using System;

namespace Maestro
{
    internal class DateTimeHandler
    {
        internal static int ConvertToUnixTimestamp(DateTime dateTime)
        {
            var unixEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)(dateTime - unixEpochStart).TotalSeconds;
        }

        internal static DateTime ConvertFromUnixTimestamp(int unixTimestamp)
        {
            var unixEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return unixEpochStart.AddSeconds(unixTimestamp);
        }
    }
}
