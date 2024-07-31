using LiteDB;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class Jwt : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: oid
        public Jwt(string primaryKey, string base64BearerToken, LiteDBHandler database) 
            : base(primaryKey, base64BearerToken, database, encoded: true)
        {
            AddProperty("bearerToken", base64BearerToken);
        }
    }
}
