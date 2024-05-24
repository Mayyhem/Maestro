using System.Collections.Generic;
using System.Dynamic;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class IntuneDevice : DynamicObject
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        public void AddProperty(string key, object value)
        {
            _properties[key] = value;
        }

        // Get all properties as a flattened dictionary
        public IDictionary<string, object> GetProperties()
        {
            var flatProperties = new Dictionary<string, object>();
            FlattenProperties(_properties, flatProperties, null);
            return flatProperties;
        }

        // Helper method to flatten properties
        private void FlattenProperties(IDictionary<string, object> source, IDictionary<string, object> target, string parentKey)
        {
            foreach (var kvp in source)
            {
                var key = parentKey == null ? kvp.Key : $"{parentKey}.{kvp.Key}";
                if (kvp.Value is IDictionary<string, object> nestedDict)
                {
                    FlattenProperties(nestedDict, target, key);
                }
                else
                {
                    target[key] = kvp.Value;
                }
            }
        }

        // Serialize the object to JSON
        public override string ToString()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(_properties);
        }
    }

    public class IntuneDeviceResponse
    {
        public List<IntuneDevice> Value { get; set; }

        // Serialize the object to JSON
        public override string ToString()
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(this);
        }
    }
}