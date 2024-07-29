using System;

namespace Maestro
{
    public class AccessToken : Credential
    {
        public string AppId { get; private set; }
        public string Audience { get; private set; }
        public JwtDynamic Jwt { get; private set; }
        public DateTime NotBefore { get; private set; }
        public string ObjectId { get; private set; }
        public string Scope { get; private set; }
        public string UserPrincipalName { get; private set; }

        public AccessToken(string base64BearerToken)
            : base("Access Token", base64BearerToken)
        {
            // Decode and parse properties of JWT
            Jwt = new JwtDynamic(base64BearerToken);

            AppId = (string)Jwt.Properties["appid"];
            Audience = (string)Jwt.Properties["aud"];
            Expiry = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["exp"]);
            NotBefore = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["nbf"]);
            ObjectId = (string)Jwt.Properties["oid"];
            Requested = DateTimeHandler.ConvertFromUnixTimestamp((int)Jwt.Properties["iat"]);
            Scope = (string)Jwt.Properties["scp"];
            TenantId = (string)Jwt.Properties["tid"];
            UserPrincipalName = (string)Jwt.Properties["upn"];
        }
    }
}
