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
            // Logging and debugging
            Logger.LogLevel logLevel = Logger.LogLevel.Info;
            if (args.Contains<string>("--debug"))
            {
                logLevel = Logger.LogLevel.Debug;
            }
            ILogger logger = new ConsoleLogger();
            Logger.Initialize(logger, logLevel);

            // Execution timer
            var timer = new Stopwatch();
            timer.Start();
            Logger.Info("Execution started");

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var httpHandler = new HttpHandler();
                var authClient = new AuthClient(httpHandler);
                await authClient.GetTenantIdAndRefreshTokenFromIntune();
                await authClient.GetIntuneAccessToken(authClient.TenantId, authClient.RefreshToken);
                //await authClient.GetEntraIdAccessToken(authClient.TenantId, authClient.RefreshToken);
                var intuneClient = new IntuneClient(authClient);
                string accessCheckResponse = await intuneClient.ListEnrolledDevicesAsync();
                if (accessCheckResponse is null) return;

                string scriptId = await intuneClient.CreateScriptPackage("Yee", "bmV0IGxvY2FsZ3JvdXAgYWRtaW5pc3RyYXRvcnM=");
                if (scriptId is null) return;
                Logger.Info($"Obtained script ID: {scriptId}");
                
            }
            catch (Exception ex)
            {
                Logger.ExceptionDetails(ex);
            }

            // Stop timer and complete execution
            timer.Stop();
            Logger.Info($"Completed execution in {timer.Elapsed}");

            // Delay completion when debugging
            if (Debugger.IsAttached)
                Console.ReadLine();
        }
    }
}
