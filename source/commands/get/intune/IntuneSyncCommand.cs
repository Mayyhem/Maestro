using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneSyncCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool reauth, int prtMethod)
        {
            var intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(database, reauth: reauth, prtMethod: prtMethod);

            if (arguments.TryGetValue("--id", out string deviceId))
            {
                await intuneClient.SyncDevice(deviceId, database);
            }
            else
            {
                Logger.Error("Missing target device ID for \"sync\" command");
                CommandLine.PrintUsage("sync");
            }
        }
    }
}
