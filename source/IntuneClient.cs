using System.Threading.Tasks;

namespace Maestro
{
    public class IntuneClient
    {
        private readonly MSGraphClient _msGraphClient;

        public IntuneClient(MSGraphClient msGraphClient)
        {
            _msGraphClient = msGraphClient;
        }

        public async Task<string> ListEnrolledDevicesAsync()
        {
            // Define the specific URI for Intune-enrolled devices
            string intuneDevicesUri = "https://graph.microsoft.com/beta/me/managedDevices";
            return await _msGraphClient.GetAsync(intuneDevicesUri);
        }
    }
}
