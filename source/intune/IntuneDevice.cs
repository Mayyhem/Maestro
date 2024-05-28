using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Maestro
{
    public class IntuneDevice : JsonObject
    {
        // Set primary key, parameterless constructor required by IDatabaseHandler.GetByProperty
        public IntuneDevice() : base("id") { }

        // Constructor that accepts a device object as a dictionary
        public IntuneDevice(Dictionary<string, object> device) : base("id", device) { }
    }
}