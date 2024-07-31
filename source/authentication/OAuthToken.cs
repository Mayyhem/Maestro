using LiteDB;

namespace Maestro
{
    public class OAuthTokenDynamic : JsonObject
    {
        // Set primary key
        public OAuthTokenDynamic(string jsonBlob, LiteDBHandler database = null) 
            : base("aadSessionId", jsonBlob, database) { }
    }
}
