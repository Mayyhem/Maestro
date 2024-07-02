using System.Threading.Tasks;

namespace Maestro
{
    public interface IAuthClient
    {
        IHttpHandler HttpHandler { get; }
        string BearerToken { get; }
        string ImpersonationToken { get; }
        string RefreshToken { get; }
        string TenantId { get; }
        Task Authenticate(string redirectUrl, IDatabaseHandler database, string prtCookie);
        Task<string> GetAccessToken(string tenantId, string portalAuthorization, string delegationTokenUrl, string extensionName, 
            string resourceName, IDatabaseHandler database);
        Task<string> GetPrtCookie();
    }
}
