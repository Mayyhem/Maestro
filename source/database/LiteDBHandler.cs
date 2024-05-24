using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool Exists<T>(string propertyName, object value) where T : class
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var query = Query.EQ(propertyName, new BsonValue(value));
            return collection.Exists(query);
        }

        public void Upsert<T>(T item, string matchProperty) where T : class
        {
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name);
            var doc = new BsonDocument();

            // Dictionary to hold all properties
            var properties = new Dictionary<string, object>();

            // If the item has a method to get dictionary properties (like GetProperties in IntuneDevice), call it
            var getPropertiesMethod = item.GetType().GetMethod("GetProperties");
            if (getPropertiesMethod != null)
            {
                var dictProperties = (IDictionary<string, object>)getPropertiesMethod.Invoke(item, null);
                foreach (var kvp in dictProperties)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            // Add properties to the document
            foreach (var kvp in properties)
            {
                doc[kvp.Key] = BsonMapper.Global.Serialize(kvp.Value);
            }

            // Get the value of the matching property
            if (properties.TryGetValue(matchProperty, out object matchValue))
            {
                // Check if an item with the same matchProperty already exists
                var existingItem = collection.FindOne(Query.EQ(matchProperty, new BsonValue(matchValue)));
                if (existingItem != null)
                {
                    // Update the existing item
                    doc["_id"] = existingItem["_id"];
                    collection.Update(doc);
                }
                else
                {
                    // Insert a new item
                    collection.Insert(doc);
                }
            }
            else
            {
                throw new ArgumentException($"Property '{matchProperty}' not found in the item.");
            }
        }
    }
}