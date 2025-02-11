using System.Collections.Generic;

namespace Maestro
{
    public class ListClientIdsCommand
    {
        public static void Execute()
        {
            // Show common client IDs
            Dictionary<string, string> clientIds = new Dictionary<string, string>
            {
                { "74658136-14ec-4630-ad9b-26e160ff0fc6", "ADIbizaUX (EntraID)" },
                { "1b730954-1685-4b74-9bfd-dac224a7b894", "Azure Active Directory PowerShell" },
                { "c44b4083-3bb0-49c1-b47d-974e53cbdf3c", "Azure Portal" },
                { "04b07795-8ddb-461a-bbee-02f9e1bf7b46", "Microsoft Azure CLI" },
                { "1950a258-227b-4e31-a9cf-717495945fc2", "Microsoft Azure PowerShell" },
                { "00000003-0000-0000-c000-000000000000", "Microsoft Graph" },
                { "5926fc8e-304e-4f59-8bed-58ca97cc39a4", "Microsoft Intune portal extension" },
            };

            Logger.Info("Common client IDs:\n");
            foreach (var clientId in clientIds)
            {
                Logger.InfoTextOnly($"{clientId.Key}:   {clientId.Value}");
            }
            System.Console.WriteLine();
        }
    }
}
