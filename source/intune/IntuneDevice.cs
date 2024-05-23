using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
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

        // Get all properties as a dictionary
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

        // Override TryGetMember to access dynamic properties
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_properties.TryGetValue(binder.Name, out result))
            {
                return true;
            }
            return base.TryGetMember(binder, out result);
        }

        // Override TrySetMember to set dynamic properties
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _properties[binder.Name] = value;
            return true;
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