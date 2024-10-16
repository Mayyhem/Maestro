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
            if (intuneClient is null) return;

            // Set default properties to print
            if (options.Properties is null)
            {
                options.Properties = new List<string> {

                    // Device info
                    "id",
                    "deviceName",
                    "managedDeviceName",
                    "managementState",
                    "enrolledDateTime",
                    "lastSyncDateTime",
                    "configurationManagerClientEnabledFeatures",
                    "model",
                    "operatingSystem",
                    "skuFamily",
                    "osVersion",

                    // Entra info
                    "joinType",
                    "azureADRegistered",
                    "deviceEnrollmentType",
                    "azureADDeviceId",
                    "deviceRegistrationState",

                    // User info
                    "userId",
                    "userPrincipalName",
                    "userDisplayName",
                    "enrolledByUserPrincipalName",
                    "usersLoggedOn",
                };
            }

            string[] properties = options.Properties.ToArray();
            await intuneClient.GetDevices(options.Id, options.Name, null, properties, options.Filter, database, true, options.Raw);
        }
    }
}
