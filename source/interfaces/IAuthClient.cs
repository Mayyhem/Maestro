namespace Maestro
{
    public interface IAuthClient
    {
        IHttpHandler HttpHandler { get; }
        string EntraIdAccessToken { get; }
        string IntuneAccessToken { get; }
        string RefreshToken { get; }
        string TenantId { get; }
    }
}