using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                if (arguments.TryGetValue("--id", out string intuneDeviceId))
                {
                    await intuneClient.DeviceQuery(kustoQuery, deviceId: intuneDeviceId, database: database);

                }
                else if (arguments.TryGetValue("--name", out string intuneDeviceName))
                {
                    await intuneClient.DeviceQuery(kustoQuery, deviceName: intuneDeviceName, database: database);
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
