using LiteDB;

namespace Maestro
{
    public class Jwt : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: oid
        public Jwt(string base64BearerToken) : base("oid", base64BearerToken) { }
        public Jwt(BsonDocument bsonDocument) : base(bsonDocument) { }
    }
}
