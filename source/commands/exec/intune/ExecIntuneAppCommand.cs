using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Maestro
{
    public class ExecIntuneAppCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Fail if neither a device or group ID is specified
            if (options.Device is null && options.Group is null)
            {
                Logger.Error("Please specify the an Intune/Entra device ID (-i) or an Entra group (-g)");
                return;
            }

            // Authenticate and get an access token for Intune
            var intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);

            // Authenticate and get an access token for EntraID 
            var entraClient = new EntraClient();
            entraClient = await EntraClient.InitAndGetAccessToken(options, database);

            string appName = options.Name;
            if (appName is null)
            {
                // Assign a random guid as the app name if not specified
                appName = "app_" + System.Guid.NewGuid();
            }

            string groupId = "";
            string newGroupId = "";

            // Find the specified group
            if (options.Group != null)
            {
                groupId = options.Group;
                EntraGroup group = await entraClient.GetGroup(groupId, null, null, database);
                if (group is null) return;
            }

            // Find the specified device
            IntuneDevice intuneDevice = null;
            if (options.Device != null)
            {
                // Check if this is an Intune device ID
                Logger.Info("Checking whether the specified device exists in Intune");
                intuneDevice = await intuneClient.GetDevice(options.Device, database: database);

                // Next, check if this is an Entra device ID or get it from the Intune device object
                if (intuneDevice is null)
                {
                    intuneDevice = await intuneClient.GetDevice(aadDeviceId: options.Device, database: database);
                };
                if (intuneDevice is null)
                {
                    Logger.Error("Failed to find the device in Intune");
                    return;
                }

                if (intuneDevice.Properties["azureADDeviceId"] == null)
                {
                    Logger.Error("Failed to identify the ID for the device in Entra");
                    return;
                }

                // Correlate the Entra device ID with the Entra object ID
                string entraDeviceId = intuneDevice.Properties["azureADDeviceId"].ToString();
                EntraDevice entraDevice = await entraClient.GetDevice(deviceDeviceId: entraDeviceId, database: database);
                if (entraDevice is null) return;

                // Create an Entra group containing the device's Entra object ID
                newGroupId = await entraClient.NewGroup(intuneDevice.Properties["deviceName"].ToString(), 
                    entraDevice.Properties["id"].ToString());
                if (newGroupId is null) return;

                groupId = newGroupId;
            }

            // Find devices in the Entra group
            List<JsonObject> groupMembers = null;
            int attempt = 1;
            int total_attempts = 6;
            while (attempt < total_attempts)
            {
                Logger.Info($"Checking Entra group members every 10 seconds, attempt {attempt} of {total_attempts}");

                groupMembers = await entraClient.GetGroupMembers(groupId, "EntraDevice");
                if (groupMembers == null)
                {
                    await Task.Delay(10000);
                    attempt++;
                }
                else
                {
                    break;
                }
            }

            // If no devices are found after the final attempt, exit
            if (groupMembers.Count == 0)
            {
                Logger.Error("No devices found in the Entra group");
                return;
            }

            // Run as system by default
            string runAsAccount = "system";
            if (options.AsUser)
            {
                // Run as logged in user if specified
                runAsAccount = "user";
            }

            // Create the app and assign it to the group
            string appId = await intuneClient.NewWin32App(groupId, appName, options.Path, runAsAccount);
            if (appId is null) return;

            if (!await intuneClient.AssignAppToGroup(appId, groupId)) return;
            Logger.Info($"App assigned to {groupId}");

            if (options.Device != null)
            {
                Logger.Info("Waiting 30 seconds before requesting device sync");
                await Task.Delay(30000);
                await intuneClient.SyncDevice(intuneDevice.Id, database, skipDeviceLookup: true);
            }

            if (options.Group != null)
            {
                Logger.Info($"Fetching all members of {groupId} for device sync");

                // Populate additional properties for the devices (e.g. deviceId)
                List<EntraDevice> entraDevices = await entraClient.GetDevices(groupMembers, printJson: false);

                // Correlate the Entra device IDs with Intune device IDs
                List<IntuneDevice> intuneDevices = await intuneClient.GetIntuneDevicesFromEntraDevices(entraDevices);

                if (intuneDevices.Count != 0)
                {
                    Logger.Info("Waiting 30 seconds before requesting device sync");
                    await Task.Delay(30000);
                    await intuneClient.SyncDevices(intuneDevices, database);
                }
                else
                {
                    Logger.Error("No devices found in Intune for the Entra group");
                    return;
                }
            }

            Logger.Info($"App with id {appId} has been deployed");

            string dbString = "";
            if (database?.Path != null)
            {
                dbString = $" -d {database.Path}";
            }
            // Always write the cleanup commands to the console
            Logger.ErrorTextOnly($"\nClean up after execution:\n    Maestro.exe delete intune app -i {appId}{dbString}");

            if (!string.IsNullOrEmpty(newGroupId))
            {
                Logger.ErrorTextOnly($"    Maestro.exe delete entra group -i {groupId}{dbString}");
            }
            Console.WriteLine();
        }
    }
}
