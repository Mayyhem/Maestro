using Microsoft.Win32;
using System;

namespace Maestro
{
    public class RefreshToken : Credential
    {
        public string ClientId { get; private set; }
        public string UserPrincipalName { get; private set; }
        public RefreshToken(string tokenBlob, string userPrincipalName, string tenantId, string clientId, DateTime? requestTimestamp = null, LiteDBHandler database = null)
            : base("Refresh Token", tokenBlob, database)
        {
            ClientId = clientId;
            TenantId = tenantId;
            UserPrincipalName = userPrincipalName;

            // Set the expiry to 90 days from the request timestamp or from now if not provided
            Requested = requestTimestamp ?? DateTime.UtcNow;
            Expiry = Requested.AddDays(90);

            if (database != null)
            {
                // Add to the RefreshToken table
                database.Upsert(this);

                // Add to Credential table
                Upsert(database);
            }
        }
    }
}
