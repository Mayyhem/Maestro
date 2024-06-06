using LiteDB;

namespace Maestro
{
    public class OAuthToken : JsonObject
    {
        // Set primary key
        public OAuthToken(string jsonBlob) : base("aadSessionId", jsonBlob) { }
        public OAuthToken(BsonDocument bsonDocument) : base(bsonDocument) { }
    }
}
