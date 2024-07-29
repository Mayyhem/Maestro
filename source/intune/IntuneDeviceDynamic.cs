using System.Collections.Generic;

namespace Maestro
{
    public class IntuneDeviceDynamic : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneDeviceDynamic(Dictionary<string, object> deviceProperties) : base("id", deviceProperties) { }
    }
}
