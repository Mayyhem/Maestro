using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Maestro
{
    public class IntuneDevice : JsonObject
    {
        public string Id;
        public string DeviceName;

        // Class instances will be stored in the collection in the database
        // Primary key: id
        public IntuneDevice(Dictionary<string, object> deviceProperties, LiteDBHandler database)
            : base("id", deviceProperties, database) 
        {
            Id = Properties["id"].ToString();
            DeviceName = Properties["deviceName"].ToString();

            Upsert(database);
        }

        public IntuneDevice(JObject jObject, LiteDBHandler database)
            : base("id", jObject.ToString(), database) 
        {
            Id = Properties["id"].ToString();
            DeviceName = Properties["deviceName"].ToString();

            Upsert(database);
        }
    }
}
