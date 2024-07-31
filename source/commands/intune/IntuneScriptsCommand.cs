using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    internal class IntuneScriptsCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly, bool reauth, int prtMethod)
        {
            var intuneClient = new IntuneClient();
            if (!databaseOnly)
            {
                intuneClient = await IntuneClient.InitAndGetAccessToken(database, reauth: reauth, prtMethod: prtMethod);
            }

            // User-specified properties
            string[] properties = new string[] { };
            if (arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                properties = propertiesCsv.Split(',');
            }

            if (arguments.TryGetValue("--id", out string scriptId))
            {
                if (databaseOnly)
                {
                    intuneClient.ShowIntuneScripts(database, properties, scriptId);
                    return;
                }
                if (arguments.TryGetValue("--delete", out string delete))
                {
                    bool deleteFlag = bool.Parse(delete);
                    if (deleteFlag)
                    {
                        await intuneClient.DeleteScriptPackage(scriptId);
                        return;
                    }
                }
                await intuneClient.GetScripts(scriptId, properties, database);
            }
            else
            {
                if (databaseOnly)
                {
                    intuneClient.ShowIntuneScripts(database, properties);
                    return;
                }
                await intuneClient.GetScripts(database: database, properties: properties);
            }
        }
    }
}
