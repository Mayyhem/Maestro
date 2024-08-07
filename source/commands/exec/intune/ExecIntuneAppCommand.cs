using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Maestro
{
    public class ExecIntuneAppCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            // Authenticate and get an access token for Intune
            var intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);

            // Authenticate and get an access token for EntraID 
            var entraClient = new EntraClient();
            entraClient = await EntraClient.InitAndGetAccessToken(options, database);

            // Find the specified group
            string groupId = options.Id;
            string appName = options.Name;
            EntraGroup group = await entraClient.GetGroup(groupId, null, null, database);
            if (group is null) return;

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

            // Find devices in the Entra group
            List<JsonObject> groupMembers = await entraClient.GetGroupMembers(groupId, "EntraDevice");
            if (groupMembers.Count == 0)
            {
                Logger.Error("No devices found in the Entra group");
                return;
            }

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

            Logger.Info($"App with id {appId} has been deployed");
        }
    }
}
