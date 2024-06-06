using LiteDB;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Dynamic;

namespace Maestro
{
    // Instances of classes derived from JsonObject can store arbitrary JSON data returned from a REST API
    // with an arbitrary number of properties (e.g., if the provider adds/removes properties).
    // The primary key must be defined when the derived class is instantiated.
    public abstract class JsonObject : DynamicObject
    {
        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        // Instantiate object from JSON string
        protected JsonObject(string primaryKey, string jsonBlob)
        {
            var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonBlob);

            foreach (var property in properties)
            {
                AddProperty(property.Key, property.Value);
            }

            if (properties.ContainsKey(primaryKey))
            {
                AddProperty("primaryKey", primaryKey);
            }

            AddProperty("jsonBlob", jsonBlob);
        }

        // Instantiate object that has already been deserialized
        protected JsonObject(string primaryKey, Dictionary<string, object> properties = null)
        {
            if (properties is null)
            {
                AddProperty("primaryKey", primaryKey);
            }
            else
            {
                foreach (var property in properties)
                {
                    AddProperty(property.Key, property.Value);
                }
                if (properties.ContainsKey(primaryKey))
                {
                    AddProperty("primaryKey", primaryKey);
                }
            }
        }

        // Instantiate object from BsonDocument fetched from the database
        protected JsonObject(BsonDocument bsonDocument)
        {
            var dict = BsonDocumentHandler.ToDictionary(bsonDocument);

            foreach (var property in dict)
            {
                AddProperty(property.Key, property.Value);
            }
        }

        public void AddProperty(string key, object value)
        {
            Properties[key] = value;
        }

        public IDictionary<string, object> GetProperties()
        {
            return Properties;
        }

        public BsonDocument ToBsonDocument()
        {
            string jsonBlob = ToJsonBlob();
            return BsonMapper.Global.Deserialize<BsonDocument>(jsonBlob);
        }

        public string ToJsonBlob()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}