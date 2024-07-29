using System.Collections.Generic;

namespace Maestro
{ 
    public class EntraUserDynamic : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public EntraUserDynamic(Dictionary<string, object> properties) : base("id", properties) { }
    }
}
