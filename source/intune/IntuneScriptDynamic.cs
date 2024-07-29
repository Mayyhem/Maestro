using System.Collections.Generic;

namespace Maestro
{
    public class IntuneScriptDynamic : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneScriptDynamic(Dictionary<string, object> properties) : base("id", properties) { }
    }
}
