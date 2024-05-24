using System.Collections.Generic;
using System.Dynamic;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class JsonObject : DynamicObject
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

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