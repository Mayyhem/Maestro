using System.Collections.Generic;
using System.Dynamic;
using System.Web.Script.Serialization;

namespace Maestro
{
    // Instances of classes derived from JsonObject can store arbitrary JSON data returned from a REST API
    // with an arbitrary number of properties (e.g., if the provider adds/removes properties).
    // The primary key must be defined when the derived class is instantiated.
    public abstract class JsonObject : DynamicObject
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected JsonObject(string primaryKey) 
        {
            AddProperty("PrimaryKey", primaryKey);
        }

        public void AddProperty(string key, object value)
        {
            _properties[key] = value;
        }

        public IDictionary<string, object> GetProperties()
        {
            return _properties;
        }

        // Serialize the object to JSON
        public override string ToString()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(_properties);
        }
    }

    public class JsonObjectResponse
    {
        public List<JsonObject> Value { get; set; }

        // Serialize the object to JSON
        public override string ToString()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(this);
        }
    }
}