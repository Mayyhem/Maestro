using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{
    public class GetIntuneDevicesCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);

            // Set default properties to print
            if (options.Properties is null)
            {
                options.Properties = new List<string> {
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

            string[] properties = options.Properties.ToArray();
            await intuneClient.GetDevices(options.Id, options.Name, null, properties, database, true);
        }
    }
}
