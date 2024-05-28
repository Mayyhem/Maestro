using System.Threading.Tasks;

namespace Maestro
{
    public interface IAuthClient
    {
        IHttpHandler HttpHandler { get; }
        string RefreshToken { get; }
        string TenantId { get; }
        Task<string> GetAccessToken(string tenantId, string portalAuthorization, string url, string extensionName, string resourceName);
        Task<(string, string)> GetTenantIdAndRefreshToken();
    }
}