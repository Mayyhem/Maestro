using System;

namespace Maestro
{
    public class RefreshToken : Credential
    {
        public string ClientId { get; private set; }
        public RefreshToken(string tokenBlob, string tenantId, string clientId, DateTime? requestTimestamp = null, LiteDBHandler database = null)
            : base("Refresh Token", tokenBlob, database)
        {
            ClientId = clientId;
            TenantId = tenantId;

            // Set the expiry to 90 days from the request timestamp or from now if not provided
            Requested = requestTimestamp ?? DateTime.UtcNow;
            Expiry = Requested.AddDays(90);

            Upsert(database);
        }
    }
}
