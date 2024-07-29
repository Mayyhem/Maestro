using LiteDB;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class JwtDynamic : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: oid
        public JwtDynamic(string base64BearerToken) : base("oid")
        {
            string decodedJson = StringHandler.DecodeJwt(base64BearerToken);
            var serializer = new JavaScriptSerializer();
            var properties = serializer.Deserialize<Dictionary<string, object>>(decodedJson);
            foreach (var property in properties)
            {
                AddProperty(property.Key, property.Value);
            }
            AddProperty("bearerToken", base64BearerToken);
        }

        public JwtDynamic(BsonDocument bsonDocument) : base(bsonDocument) { }
    }
}
