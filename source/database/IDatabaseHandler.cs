using LiteDB;
using System.Collections.Generic;

namespace Maestro
{
    public interface IDatabaseHandler
    {
        void Dispose();
        BsonDocument FindByPrimaryKey<T>(string primaryKeyValue);
        IEnumerable<BsonDocument> FindInCollection<T>(string propertyName = "", BsonValue propertyValue = null);
        string FindValidJwt(string scope = "");
        string FindValidOAuthToken();
        void Upsert<T>(T item) where T : JsonObject;
    }
}