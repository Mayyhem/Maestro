using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Maestro
{
    public class IntuneApp : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneApp(Dictionary<string, object> properties, LiteDBHandler database)
            : base("id", properties, database) { }

        public IntuneApp(JObject jObject, LiteDBHandler database)
            : base("id", jObject.ToString(), database) { }
    }
}
