using System.Collections.Generic;

namespace Maestro
{
    public class ListResourceIdsCommand
    {
        public static void Execute()
        {
            // Show common resource IDs
            Dictionary<string, string> resources = new Dictionary<string, string>
            {
                { "https://graph.microsoft.com", "" }
            };

            Logger.Info("Common resource IDs:");
            foreach (var resource in resources)
            {
                Logger.InfoTextOnly($"{resource.Key}: {resource.Value}");
            }
        }
    }
}
