using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneExecAppCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool reauth)
        {
            if (arguments.TryGetValue("--id", out string deviceId) && arguments.TryGetValue("--name", out string appName) 
                && arguments.TryGetValue("--path", out string installationPath))
            {
                // Run as system by default
                string runAsAccount = "system";

                // Run as logged in user if specified
                if (arguments.ContainsKey("--run-as-user"))
                {
                    runAsAccount = "user";
                }

                var intuneClient = new IntuneClient();
                intuneClient = await IntuneClient.InitAndGetAccessToken(database, reauth: reauth);

                IntuneDevice device = await intuneClient.GetDevice(deviceId, database: database);
                if (device is null) return;

                string win32AppId = await intuneClient.NewWin32App(deviceId, appName, installationPath, runAsAccount);
                if (win32AppId is null) return;

                //string assignmentId = await intuneClient.NewAppAssignment(win32AppId, deviceId);
                //if (assignmentId is null) return;

                //Logger.Info("App assignment created, waiting 10 seconds before requesting device sync");
                //Thread.Sleep(10000);
                //await intuneClient.SyncDevice(deviceId, database, skipDeviceLookup: true);
                
            }
            else
            {
                Logger.Error("Missing arguments for \"app\" command");
                CommandLine.PrintUsage("app");
            }
        }
    }
}
