using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public class GetIntuneScriptsCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);
            if (intuneClient is null) return;

            // Set default properties to print
            if (options.Properties is null)
            {
                options.Properties = new List<string> {
                    "id",
                    "displayName",
                    "description",
                    "detectionScriptContent",
                    "remediationScriptContent",
                    "createdDateTime",
                    "lastModifiedDateTime",
                    "runAsAccount",
                };
            }

            string[] properties = options.Properties.ToArray();
            await intuneClient.GetScripts(options.Id, options.Name, properties, options.Filter, database, 
                true, options.Raw);
        }
    }
}
