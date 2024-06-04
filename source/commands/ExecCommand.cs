using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    internal class ExecCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            if (arguments.TryGetValue("subcommand", out string subcommandName))
            {
                if (subcommandName == "sync")
                {
                    await ExecSyncCommand.Execute(arguments, database);
                }
            }
            else if (arguments.TryGetValue("--id", out string deviceId) && arguments.TryGetValue("--script", out string script))
            {
                IntuneClient intuneClient = await IntuneClient.CreateAndGetToken(database);
                IntuneDevice device = await intuneClient.GetDevice(deviceId, database: database);

                string filterId = await intuneClient.NewDeviceAssignmentFilter(deviceId);
                if (filterId is null) return;

                string scriptId = await intuneClient.NewScriptPackage("LiveDemoHoldMyBeer", script);
                if (scriptId is null) return;

                await intuneClient.NewDeviceManagementScriptAssignmentHourly(filterId, scriptId);
                await intuneClient.SyncDevice(deviceId, database, skipDeviceLookup: true);
            }
            else
            {
                Logger.Error("Missing arguments for \"exec\" command");
                CommandLine.PrintUsage("exec");
            }
        }
    }
}
