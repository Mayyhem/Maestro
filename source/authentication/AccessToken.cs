using System;

namespace Maestro
{
    public class AccessToken : Credential
    {
        public string Audience { get; private set; }

        // appid
        public string ClientId { get; private set; }
        public Jwt Jwt { get; private set; }
        public DateTime NotBefore { get; private set; }
        public string ObjectId { get; private set; }
        public string Scope { get; private set; }
        public string UserPrincipalName { get; private set; }

        public AccessToken(string base64BearerToken, LiteDBHandler database)
            : base("Access Token", base64BearerToken, database)
        {
            // Decode and parse properties of JWT and store in database
            Jwt = new Jwt("oid", base64BearerToken, database);

            ClientId = (string)Jwt.Properties["appid"];
            Audience = (string)Jwt.Properties["aud"];
            Expiry = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["exp"]);
            NotBefore = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["nbf"]);
            ObjectId = (string)Jwt.Properties["oid"];
            Requested = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["iat"]);
            Scope = (string)Jwt.Properties["scp"];
            TenantId = (string)Jwt.Properties["tid"];
            UserPrincipalName = (string)Jwt.Properties["upn"];

            if (database != null)
            {
                // Add to the AccessToken table
                database.Upsert(this);

                // Add to Credential table
                Upsert(database);
            }
        }
    }
}
