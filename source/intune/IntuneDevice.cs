using LiteDB;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Maestro
{
    public class IntuneDevice : JsonObject
    {
        // Class instances will be stored in the IntuneDevice collection in the database
        // Primary key: id
        public IntuneDevice(Dictionary<string, object> deviceProperties) : base("id", deviceProperties) { }

        public BsonDocument ToBsonDocument()
        {
            string jsonBlob = ToJsonBlob();
            return BsonMapper.Global.Deserialize<BsonDocument>(jsonBlob);
        }

        public string ToJsonBlob()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
