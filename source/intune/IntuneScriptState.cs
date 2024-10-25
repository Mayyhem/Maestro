using System.Collections.Generic;

namespace Maestro
{
    public class IntuneScriptState : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneScriptState(Dictionary<string, object> properties, LiteDBHandler database)
            : base("id", properties, database)
        {
        }
    }
}