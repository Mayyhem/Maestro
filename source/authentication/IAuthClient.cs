using System.Threading.Tasks;

namespace Maestro
{
    public interface IAuthClient
    {
        IHttpHandler HttpHandler { get; }
        string RefreshToken { get; }
        string TenantId { get; }
        Task<string> GetAccessToken(string tenantId, string portalAuthorization, string delegationTokenUrl, string extensionName, 
            string resourceName, IDatabaseHandler database);
        Task Authenticate(string redirectUrl, IDatabaseHandler database);
    }
}
