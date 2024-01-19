using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();
            Logger.Initialize(logger, Logger.LogLevel.Debug);
            Logger.Info("Execution started");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var httpHandler = new HttpHandler();
            var authClient = new AuthClient(httpHandler);
            string intuneAccessToken = await authClient.GetIntuneAccessToken();
            httpHandler.SetAuthorizationHeader(intuneAccessToken);
            var msgraphClient = new MSGraphClient(httpHandler);
            var intuneClient = new IntuneClient(msgraphClient);
            await intuneClient.ListEnrolledDevicesAsync();

            // Debugging
            Console.ReadLine();
        }
    }
}
