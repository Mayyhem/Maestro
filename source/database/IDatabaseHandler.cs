using LiteDB;
using System.Collections.Generic;

namespace Maestro
{
    public interface IDatabaseHandler
    {
        BsonDocument FindByPrimaryKey<T>(string primaryKeyValue);
        IEnumerable<BsonDocument> FindInCollection<T>(string propertyName = "", BsonValue propertyValue = null);
        BsonDocument FindValidJwt<T>(string scope = "");
        void Upsert<T>(T item) where T : JsonObject;
    }
}