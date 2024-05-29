using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal static class GetDevices
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool databaseOnly = false)
        {
            var intuneClient = new IntuneClient();

            if (arguments.TryGetValue("--id", out string intuneDeviceId))
            {
                await intuneClient.GetDevices(deviceId: intuneDeviceId, database: database, databaseOnly: databaseOnly);
            }
            else if (arguments.TryGetValue("--name", out string intuneDeviceName))
            {
                await intuneClient.GetDevices(deviceName: intuneDeviceName, database: database, databaseOnly: databaseOnly);
            }  
            else
            {
               await intuneClient.GetDevices(database: database, databaseOnly: databaseOnly);
            }
        }
    }
}
