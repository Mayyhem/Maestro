using LiteDB;
using System.Collections.Generic;
using System.Reflection;

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

        public ILiteCollection<T> GetCollection<T>(string key)
        {
            return _database.GetCollection<T>(key);
        }

        public bool Exists<T>(string propertyName, object value) where T : class
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var query = Query.EQ(propertyName, new BsonValue(value));
            return collection.Exists(query);
        }

        public void Upsert<T>(T item) where T : class
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var doc = new BsonDocument();

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                var value = prop.GetValue(item);
                doc[prop.Name] = new BsonValue(value);
            }

            collection.Upsert(doc);
        }
    }
}