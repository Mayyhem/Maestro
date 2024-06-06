using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneSyncCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database, bool reauth)
        {
            IntuneClient intuneClient = await IntuneClient.CreateAndGetToken(database, reauth: reauth);

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
