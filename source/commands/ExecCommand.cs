using System;
using System.Collections.Generic;
using System.Net;
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
                Console.WriteLine($"Target: {deviceId}, Script: {script}");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var httpHandler = new HttpHandler();
                var authClient = new AuthClient(httpHandler);
                await authClient.GetTenantIdAndRefreshToken();

                var intuneClient = new IntuneClient(authClient);
                await intuneClient.GetAccessToken(authClient.TenantId, authClient.RefreshToken);

                string filterId = await intuneClient.NewDeviceAssignmentFilter(deviceId);
                if (filterId is null) return;

                string scriptId = await intuneClient.NewScriptPackage("LiveDemoHoldMyBeer", script);
                if (scriptId is null) return;

                await intuneClient.NewDeviceManagementScriptAssignmentHourly(filterId, scriptId);
                await intuneClient.SyncDevice(deviceId, database);
            }
            else
            {
                Logger.Error("Missing arguments for \"exec\" command");
                CommandLine.PrintUsage("exec");
            }
        }
    }
}