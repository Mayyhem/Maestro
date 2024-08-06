using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Maestro
{
    public class IntuneApp : JsonObject
    {
        public string Id;
        public string DisplayName;

        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneApp(Dictionary<string, object> properties, LiteDBHandler database)
            : base("id", properties, database) 
        {
            Id = Properties["id"].ToString();
            DisplayName = Properties["displayName"].ToString();
        }

        public IntuneApp(JObject jObject, LiteDBHandler database)
            : base("id", jObject.ToString(), database) 
        {
            Id = Properties["id"].ToString();
            DisplayName = Properties["displayName"].ToString();
        }
    }
}
