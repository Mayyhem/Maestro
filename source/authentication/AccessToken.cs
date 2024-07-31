using System;

namespace Maestro
{
    public class AccessToken : Credential
    {
        public string AppId { get; private set; }
        public string Audience { get; private set; }
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

            AppId = (string)Jwt.Properties["appid"];
            Audience = (string)Jwt.Properties["aud"];
            Expiry = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["exp"]);
            NotBefore = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["nbf"]);
            ObjectId = (string)Jwt.Properties["oid"];
            Requested = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["iat"]);
            Scope = (string)Jwt.Properties["scp"];
            TenantId = (string)Jwt.Properties["tid"];
            UserPrincipalName = (string)Jwt.Properties["upn"];

            Upsert(database);
        }
    }
}
