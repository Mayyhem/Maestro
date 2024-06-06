using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal static class EntraGroupsCmdHandler
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool databaseOnly = false)
        {
            EntraClient entraClient = new EntraClient();

            IntuneClient intuneClient = await IntuneClient.CreateAndGetToken(database);
            string[] properties = null;

            if (arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                properties = propertiesCsv.Split(',');
            }

            if (arguments.TryGetValue("--id", out string intuneDeviceId))
            {
                await intuneClient.GetIntuneDevices(deviceId: intuneDeviceId, properties: properties, database: database);
            }
            else if (arguments.TryGetValue("--name", out string intuneDeviceName))
            {
                await intuneClient.GetIntuneDevices(deviceName: intuneDeviceName, properties: properties, database: database);
            }  
            else
            {
               // Get information from all devices by default when no options are provided
               await intuneClient.GetIntuneDevices(properties: properties, database: database);
            }
        }
    }
}
