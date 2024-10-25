using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maestro
{
    public class GetIntuneScriptOutputCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            if (options.Id is null)
            {
                Logger.Error("Please specify a script ID (-i)");
                CommandLine.PrintUsage($"{options.FullCommand}");
                return;
            }

            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            if (intuneClient is null) return;

            await intuneClient.GetScriptOutput(options.Device, options.Id, options.Timeout, options.Wait, 
                options.Properties?.ToArray(), options.Filter, database, true, options.Raw);
        }
    }
}
