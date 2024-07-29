using LiteDB;

namespace Maestro
{
    public class OAuthTokenDynamic : JsonObject
    {
        // Set primary key
        public OAuthTokenDynamic(string jsonBlob) : base("aadSessionId", jsonBlob) { }
        public OAuthTokenDynamic(BsonDocument bsonDocument) : base(bsonDocument) { }
    }
}
