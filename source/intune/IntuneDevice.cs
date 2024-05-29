using System.Collections.Generic;

namespace Maestro
{
    public class IntuneDevice : JsonObject
    {
        // Class instances will be stored in the IntuneDevice collection in the database
        // Primary key: id
        public IntuneDevice(Dictionary<string, object> deviceProperties) : base("id", deviceProperties) { }
    }
}