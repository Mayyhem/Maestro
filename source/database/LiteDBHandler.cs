using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Maestro
{
    internal class LiteDBHandler : IDatabaseHandler
    {
        private readonly LiteDatabase _database;
        public LiteDBHandler(string databasePath)
        {
            _database = new LiteDatabase(databasePath);
        }

        public IEnumerable<BsonDocument> FindInCollection<T>(string propertyName, BsonValue propertyValue)
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var query = Query.EQ(propertyName, propertyValue);
            return collection.Find(query);
        }

        public BsonDocument FindByPrimaryKey<T>(string primaryKeyValue)
        {
            return _database.GetCollection<BsonDocument>(typeof(T).Name).FindById(new BsonValue(primaryKeyValue));
        }

        public BsonDocument FindValidJwt<T>(string scope = "")
        {
            // Get the Jwt collection (or create, if doesn't exist)
            var collection = _database.GetCollection<BsonDocument>("Jwt");

            // Define current Unix timestamp
            var nowUnixTimestamp = DateTimes.ConvertToUnixTimestamp(DateTime.UtcNow);

            // Use a single query to filter documents and find the matching JWT
            var farthestExpJwt = collection.FindAll()
                .Where(doc =>
                    doc.ContainsKey("nbf") &&
                    doc.ContainsKey("exp") &&
                    doc["nbf"].IsInt32 &&
                    doc["exp"].IsInt32 &&
                    // Check if the JWT is currently valid
                    doc["nbf"].AsInt32 <= nowUnixTimestamp && doc["exp"].AsInt32 >= nowUnixTimestamp &&
                    // Check if the JWT has the required scope, if provided
                    (string.IsNullOrEmpty(scope) ||
                        (doc.ContainsKey("scp") &&
                        doc["scp"].IsString &&
                        doc["scp"].AsString.Split(' ').Contains(scope))))
                // Find the JWT with the farthest expiration date
                .OrderByDescending(doc => doc["exp"].AsInt32)
                .FirstOrDefault();

            return farthestExpJwt;
        }

        // Upsert dynamic objects with unknown properties using PrimaryKey object property value as _id
        public void Upsert<T>(T item) where T : JsonObject
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var doc = new BsonDocument();

            // Get the properties dynamically
            var properties = item.GetProperties();

            // Find the primary key property
            var primaryKeyName = properties["primaryKey"]?.ToString();
            string primaryKeyValue = "";

            foreach (var kvp in properties)
            {
                doc[kvp.Key] = BsonMapper.Global.Serialize(kvp.Value);
                if (kvp.Key == primaryKeyName)
                {
                    primaryKeyValue = (string)kvp.Value;
                }
            }

            // Ensure the primary key is set as _id in BsonDocument
            doc["_id"] = new BsonValue(primaryKeyValue);
            collection.Upsert(doc);
            Logger.Debug($"Upserted item in database: {typeof(T).Name}");
        }
    }
}