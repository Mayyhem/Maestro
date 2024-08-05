using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maestro
{
    public static class GetIntuneAppsCommand
    {
        public static async Task Execute(CommandLineOptions options, LiteDBHandler database)
        {
            IntuneClient intuneClient = new IntuneClient();
            intuneClient = await IntuneClient.InitAndGetAccessToken(options, database);

            // Set default properties to print
            if (options.Properties is null)
            {
                options.Properties = new List<string> {
                    "id",
                    "displayName",
                    "description",
                    "publisher",
                    "createdDateTime",
                    "lastModifiedDateTime",
                    "publishingState",
                    "isAssigned"
                };
            }

            string[] properties = options.Properties.ToArray();
            await intuneClient.GetApps(options.Id, options.Name, properties, database, true);


            /*
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
            */
        }
    }
}
