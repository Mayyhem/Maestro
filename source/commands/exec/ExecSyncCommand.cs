using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class ExecSyncCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, IDatabaseHandler database)
        {
            var intuneClient = new IntuneClient();

            if (arguments.TryGetValue("--id", out string intuneDeviceId))
            {
                await intuneClient.SyncDevice(intuneDeviceId, database);
            }
            else
            {
                Logger.Error("Missing target device ID for \"sync\" command");
                CommandLine.PrintUsage("exec");
            }
        }
    }
}
