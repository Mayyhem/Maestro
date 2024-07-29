using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal static class IntuneDevicesCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly = false, bool reauth = false)
        {
            IntuneClient intuneClient = new IntuneClient();
            if (!databaseOnly)
            {
                intuneClient = await IntuneClient.InitAndGetAccessToken(database, reauth: reauth);
            }

            // Set default properties to print
            string[] properties = new[] {
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

            // User-specified properties
            if (arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                properties = propertiesCsv.Split(',');
            }

            // Filter objects
            if (arguments.TryGetValue("--id", out string intuneDeviceId))
            {
                if (databaseOnly)
                {
                    intuneClient.ShowIntuneDevices(database, properties, intuneDeviceId);
                    return;
                }
                await intuneClient.GetDevices(deviceId: intuneDeviceId, properties: properties, database: database);
            }
            else if (arguments.TryGetValue("--name", out string intuneDeviceName))
            {
                if (databaseOnly)
                {
                    intuneClient.ShowIntuneDevices(database, properties, deviceName: intuneDeviceName);
                    return;
                }
                await intuneClient.GetDevices(deviceName: intuneDeviceName, properties: properties, database: database);
            }

            // Get information from all devices by default
            else
            {
                if (databaseOnly)
                {
                    intuneClient.ShowIntuneDevices(database, properties);
                    return;
                }
                await intuneClient.GetDevices(database: database, properties: properties);
            }
        }
    }
}
