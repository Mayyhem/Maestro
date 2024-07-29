using System.Collections.Generic;

namespace Maestro
{ 
    public class EntraGroupDynamic : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public EntraGroupDynamic(Dictionary<string, object> properties) : base("id", properties) { }
    }
}
