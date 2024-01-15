using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Maestro
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string accessToken = args[0];

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var msGraphClient = new MSGraphClient(httpClient);
            var intuneClient = new IntuneClient(msGraphClient);

            try
            {
                var devices = await intuneClient.ListEnrolledDevicesAsync();
                Console.WriteLine(devices);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred:");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                // In a production environment, consider logging the error instead
                // LogError(e);
            }

            Console.ReadLine();
        }
    }
}
