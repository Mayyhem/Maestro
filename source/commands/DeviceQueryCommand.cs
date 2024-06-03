using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class DeviceQueryCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            var intuneClient = new IntuneClient();

            if (arguments.TryGetValue("--query", out string kustoQuery))
            {
                // Retry request for query results X times by default
                int maxRetries = 10;
                // Wait X seconds between retries by default
                int retryDelay = 3;

                if (arguments.TryGetValue("--retries", out string retriesString))
                {
                    maxRetries = int.Parse(retriesString);
                }
                if (arguments.TryGetValue("--wait", out string retryDelayString))
                {
                    retryDelay = int.Parse(retryDelayString);
                }
                if (arguments.TryGetValue("--id", out string deviceId))
                {
                    await intuneClient.ExecuteDeviceQuery(kustoQuery, maxRetries, retryDelay, deviceId: deviceId, database: database);
                }
                else if (arguments.TryGetValue("--name", out string deviceName))
                {
                    await intuneClient.ExecuteDeviceQuery(kustoQuery, maxRetries, retryDelay, deviceName: deviceName, database: database);
                }
                else
                {
                    Logger.Error("Missing target for \"devicequery\" command");
                    CommandLine.PrintUsage("devicequery");
                }
            }
            else 
            {
                Logger.Error("Missing query for \"devicequery\" command");
                CommandLine.PrintUsage("devicequery");
            }
        }
    }
}
