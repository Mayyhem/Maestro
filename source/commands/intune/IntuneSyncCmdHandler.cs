using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneSyncCmdHandler
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            IntuneClient intuneClient = await IntuneClient.CreateAndGetToken(database);

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
