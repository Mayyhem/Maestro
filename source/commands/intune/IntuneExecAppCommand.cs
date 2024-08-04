using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneExecAppCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool reauth)
        {
            if (arguments.TryGetValue("--id", out string groupId) && arguments.TryGetValue("--name", out string appName) 
                && arguments.TryGetValue("--path", out string installationPath))
            {
                // Run as system by default
                string runAsAccount = "system";

                // Run as logged in user if specified
                if (arguments.ContainsKey("--run-as-user"))
                {
                    runAsAccount = "user";
                }

                // Authenticate and get an access token for Intune
                var intuneClient = new IntuneClient();
                intuneClient = await IntuneClient.InitAndGetAccessToken(database, reauth: reauth);

                // Authenticate and get an access token for EntraID 
                var entraClient = new EntraClient();
                entraClient = await EntraClient.InitAndGetAccessToken(database, reauth: reauth, accessTokenMethod: 1);

                // Find the specified group
                EntraGroup group = await entraClient.GetGroup(groupId, database: database);
                if (group is null) return;

                // Create the app and assign it to the group
                string appId = await intuneClient.NewWin32App(groupId, appName, installationPath, runAsAccount);
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
                    Logger.Info("Waiting 10 seconds before requesting device sync");
                    await Task.Delay(10000);
                    await intuneClient.SyncDevices(intuneDevices, database);
                }
                else
                {
                    Logger.Info("No devices found in Intune for the Entra group");
                }

                // Delete the application
                await intuneClient.DeleteApplication(appId);
            }
            else
            {
                Logger.Error("Missing arguments for \"app\" command");
                CommandLine.PrintUsage("app");
            }
        }
    }
}
