using System;

namespace Maestro
{
    public class OAuthTokenResponse : JsonObject
    {
        public AccessToken AccessToken { get; private set; }
        public DateTime ExpiresIn { get; private set; }
        public string IdToken { get; private set; }
        public RefreshToken RefreshToken { get; private set; }
        public string Scope { get; private set; }
        public string TokenType { get; private set; }


    // Set primary key
        public OAuthTokenResponse(string jsonBlob, LiteDBHandler database = null)
        : base("access_token", jsonBlob, database) 
        {
            TokenType = (string)Properties["token_type"];
            Scope = (string)Properties["scope"];
            ExpiresIn = DateTime.UtcNow.AddSeconds((long)Properties["expires_in"]);
            AccessToken = new AccessToken((string)Properties["access_token"], database);
            RefreshToken = new RefreshToken((string)Properties["refresh_token"], AccessToken.UserPrincipalName,
                AccessToken.TenantId, AccessToken.ClientId, AccessToken.Requested, database);
            IdToken = (string)Properties["id_token"];

            Upsert(database);
        }
    }
}
