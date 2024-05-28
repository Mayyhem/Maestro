using LiteDB;
using System.Collections.Generic;

namespace Maestro
{
    internal class LiteDBHandler : IDatabaseHandler
    {
        private readonly LiteDatabase _database;
        public LiteDBHandler(string databasePath)
        {
            _database = new LiteDatabase(databasePath);
        }

        public List<T> GetAll<T>() where T : class, new()
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var items = new List<T>();

            foreach (var doc in collection.FindAll())
            {
                var item = new T();
                foreach (var prop in typeof(T).GetProperties())
                {
                    if (doc.TryGetValue(prop.Name, out BsonValue value))
                    {
                        prop.SetValue(item, value.RawValue);
                    }
                }
                items.Add(item);
            }

            return items;
        }

        public List<T> GetByProperty<T>(string propertyName, object value) where T : class, new()
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var items = new List<T>();

            var query = Query.EQ(propertyName, new BsonValue(value));
            var results = collection.Find(query);

            foreach (var doc in results)
            {
                var item = BsonMapper.Global.ToObject<T>(doc);
                items.Add(item);
            }

            return items;
        }

        public bool Exists<T>(string propertyName, object value) where T : class
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var query = Query.EQ(propertyName, new BsonValue(value));
            return collection.Exists(query);
        }

        // Upsert dynamic objects with unknown properties using PrimaryKey object property value as _id
        public void Upsert<T>(T item) where T : JsonObject
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var doc = new BsonDocument();

            // Get the properties dynamically
            var properties = item.GetProperties();

            // Find the primary key property
            var primaryKeyName = properties["PrimaryKey"]?.ToString();
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