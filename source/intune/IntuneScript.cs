using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Maestro
{
    public class IntuneScript : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneScript(Dictionary<string, object> properties, LiteDBHandler database) 
            : base("id", properties, database) { }

        public IntuneScript(JObject jObject, LiteDBHandler database)
            : base("id", jObject.ToString(), database) { }
    }
}
