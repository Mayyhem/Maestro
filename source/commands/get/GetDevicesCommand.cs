using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal static class GetDevicesCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool databaseOnly = false)
        {
            var intuneClient = new IntuneClient();
            string[] properties = null;

            if (arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                properties = propertiesCsv.Split(',');
            }

            // Use default properties if none were provided
            if (properties == null || properties.Length == 0)
            {
                properties = new[] { 
                    "id", 
                    "deviceName", 
                    "managementState",
                    "lastSyncDateTime",
                    "operatingSystem",
                    "osVersion",
                    "azureADRegistered",
                    "deviceEnrollmentType",
                    "azureActiveDirectoryDeviceId",
                    "deviceRegistrationState",
                    "model",
                    "managedDeviceName",
                    "joinType",
                    "skuFamily",
                    "usersLoggedOn"
                };
            }

            if (arguments.TryGetValue("--id", out string intuneDeviceId))
            {
                await intuneClient.GetDevices(deviceId: intuneDeviceId, properties: properties, database: database, databaseOnly: databaseOnly);
            }
            else if (arguments.TryGetValue("--name", out string intuneDeviceName))
            {
                await intuneClient.GetDevices(deviceName: intuneDeviceName, properties: properties, database: database, databaseOnly: databaseOnly);
            }  
            else
            {
               // Get information from all devices by default
               await intuneClient.GetDevices(properties: properties, database: database, databaseOnly: databaseOnly);
            }
        }
    }
}
