using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneExecScriptCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool reauth)
        {
            if (arguments.TryGetValue("--id", out string deviceId) && arguments.TryGetValue("--script", out string script))
            {
                var intuneClient = new IntuneClient();
                intuneClient = await IntuneClient.InitAndGetAccessToken(database, reauth: reauth);

                IntuneDevice device = await intuneClient.GetDevice(deviceId, database: database);

                string filterId = await intuneClient.NewDeviceAssignmentFilter(deviceId);
                if (filterId is null) return;

                string scriptId = await intuneClient.NewScriptPackage("LiveDemoHoldMyBeer", script);
                if (scriptId is null) return;

                await intuneClient.NewDeviceManagementScriptAssignment(filterId, scriptId);
                Logger.Info("Script assignment created, waiting 10 seconds before requesting device sync");
                Thread.Sleep(10000);
                await intuneClient.SyncDevice(deviceId, database, skipDeviceLookup: true);
            }
            else
            {
                Logger.Error("Missing arguments for \"script\" command");
                CommandLine.PrintUsage("script");
            }
        }
    }
}
