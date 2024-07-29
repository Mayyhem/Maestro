using System;

namespace Maestro
{
    public class RefreshToken : Credential
    {
        public string ClientId { get; private set; }
        public RefreshToken(string blob, string tenantId, string clientId, DateTime? requestTimestamp = null)
            : base("Refresh Token", blob)
        {
            ClientId = clientId;
            Requested = requestTimestamp ?? DateTime.UtcNow;
            // Set the expiry to 90 days from the request timestamp or from now if not provided
            Expiry = Requested.AddDays(90);
            TenantId = tenantId;
        }
    }
}
