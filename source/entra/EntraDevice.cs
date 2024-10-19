using System.Collections.Generic;

namespace Maestro
{
    public class EntraDevice : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public EntraDevice(Dictionary<string, object> deviceProperties, LiteDBHandler database)
            : base("id", deviceProperties, database) {
        }
    }
}
