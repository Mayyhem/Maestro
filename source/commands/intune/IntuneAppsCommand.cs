using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public static class IntuneAppsCommand
    {
        public static async Task Execute(Dictionary<string, string> arguments, LiteDBHandler database, bool databaseOnly, bool reauth, int prtMethod)
        {
            IntuneClient intuneClient = new IntuneClient();
            if (!databaseOnly)
            {
                intuneClient = await IntuneClient.InitAndGetAccessToken(database, reauth: reauth, prtMethod: prtMethod);
            }

            // Set default properties to print
            string[] properties = new[] {
                    "id",
                    "displayName",
                    "description",
                    "publisher",
                    "createdDateTime",
                    "lastModifiedDateTime",
                    "publishingState",
                    "isAssigned"
                };

            // User-specified properties
            if (arguments.TryGetValue("--properties", out string propertiesCsv))
            {
                properties = propertiesCsv.Split(',');
            }

            // Filter objects
            if (arguments.TryGetValue("--id", out string intuneAppId))
            {
                if (databaseOnly)
                {
                    Logger.Error("This feature has not yet been implemented");
                    //intuneClient.ShowIntuneDevices(database, properties, intuneAppId);
                    return;
                }
                if (arguments.TryGetValue("--delete", out string delete))
                {
                    bool deleteFlag = bool.Parse(delete);
                    if (deleteFlag)
                    {
                        await intuneClient.DeleteApplication(intuneAppId);
                        return;
                    }
                }
                await intuneClient.GetApps(appId: intuneAppId, properties: properties, database: database);

            }
            else if (arguments.TryGetValue("--name", out string intuneAppName))
            {
                if (databaseOnly)
                {
                    Logger.Error("This feature has not yet been implemented");
                    //intuneClient.ShowIntuneDevices(database, properties, deviceName: intuneAppName);
                    return;
                }
                
                await intuneClient.GetApps(appName: intuneAppName, properties: properties, database: database);
            }

            // Get information from all devices by default
            else
            {
                if (databaseOnly)
                {
                    Logger.Error("This feature has not yet been implemented");
                    //intuneClient.ShowIntuneDevices(database, properties);
                    return;
                }
                await intuneClient.GetApps(database: database, properties: properties);
            }
        }
    }
}
