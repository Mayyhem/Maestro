using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Maestro
{
    public class IntuneScript : JsonObject
    {
        // Fields to always display
        public string Id;
        public string DisplayName;

        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneScript(Dictionary<string, object> properties, LiteDBHandler database) 
            : base("id", properties, database) 
        {
            Id = Properties["id"].ToString();
            DisplayName = Properties["displayName"].ToString();
        }

        public IntuneScript(JObject jObject, LiteDBHandler database)
            : base("id", jObject.ToString(), database)
        {
            Id = Properties["id"].ToString();
            DisplayName = Properties["displayName"].ToString();
        }
    }
}
