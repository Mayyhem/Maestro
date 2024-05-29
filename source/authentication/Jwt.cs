using LiteDB;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class Jwt : JsonObject
    {
        // Class instances will be stored in the Jwt collection in the database
        // Primary key: oid
        public Jwt(string base64BearerToken) : base("oid") 
        {
            string decodedJson = Strings.DecodeJwt(base64BearerToken);
            var serializer = new JavaScriptSerializer();
            var properties = serializer.Deserialize<Dictionary<string, object>>(decodedJson);

            foreach (var property in properties)
            {
                AddProperty(property.Key, property.Value);
            }
            AddProperty("bearerToken", base64BearerToken);
        }

        public Jwt(BsonDocument bsonDocument) : base(bsonDocument) { }
    }
}
