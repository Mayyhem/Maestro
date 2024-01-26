using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Maestro
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                if (args.Length < 2)
                    Util.DisplayUsageAndExit();

                // Logging and debugging
                Logger.LogLevel logLevel = Logger.LogLevel.Info;
                if (args.Contains<string>("--debug"))
                {
                    logLevel = Logger.LogLevel.Debug;
                }
                ILogger logger = new ConsoleLogger();
                Logger.Initialize(logger, logLevel);

                string deviceName = args[0];
                string scriptContents = args[1];

                // Execution timer
                var timer = new Stopwatch();
                timer.Start();
                Logger.Info("Execution started");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var httpHandler = new HttpHandler();
                var authClient = new AuthClient(httpHandler);
                await authClient.GetTenantIdAndRefreshToken();

                var intuneClient = new IntuneClient(authClient);
                await intuneClient.GetAccessToken(authClient.TenantId, authClient.RefreshToken);

                string filterId = await intuneClient.NewDeviceAssignmentFilter(deviceName);
                if (filterId is null) return;

                string scriptId = await intuneClient.NewScriptPackage("Yeehaw2", scriptContents);
                if (scriptId is null) return;

                await intuneClient.NewDeviceManagementScriptAssignmentHourly(filterId, scriptId);

                //string deviceId = "";
                //await intuneClient.SyncDevice();
                
                // Stop timer and complete execution
                timer.Stop();
                Logger.Info($"Completed execution in {timer.Elapsed}");

            }
            catch (Exception ex)
            {
                Logger.ExceptionDetails(ex);
            }

            // Delay completion when debugging
            if (Debugger.IsAttached)
                Console.ReadLine();
        }
    }
}
