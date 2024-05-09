using System;
using System.Collections.Generic;
using System.Net;

namespace Maestro
{
    internal class ExecCommand
    {
        public static async void Execute(Dictionary<string, string> arguments)
        {
            // Implementation for exec command
            Console.WriteLine("Executing 'exec' command...");
            if (arguments.TryGetValue("target", out string target) && arguments.TryGetValue("script", out string script))
            {
                Console.WriteLine($"Target: {target}, Script: {script}");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var httpHandler = new HttpHandler();
                var authClient = new AuthClient(httpHandler);
                await authClient.GetTenantIdAndRefreshToken();

                var intuneClient = new IntuneClient(authClient);
                await intuneClient.GetAccessToken(authClient.TenantId, authClient.RefreshToken);

                string filterId = await intuneClient.NewDeviceAssignmentFilter(target);
                if (filterId is null) return;

                string scriptId = await intuneClient.NewScriptPackage("LiveDemoHoldMyBeer", script);
                if (scriptId is null) return;

                await intuneClient.NewDeviceManagementScriptAssignmentHourly(filterId, scriptId);
                await intuneClient.SyncDevice(target);
            }
            else
            {
                Console.WriteLine("Missing arguments for 'exec' command.");
                CommandLine.PrintExecUsage();
            }
        }
    }
}
