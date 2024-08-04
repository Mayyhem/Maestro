using System.Collections.Generic;

namespace Maestro
{
    public class EntraDevice : JsonObject
    {
        public string Name { get; private set; }
        public string DeviceId { get; private set; }
        public string ObjectId { get; private set; }
        public bool Enabled { get; private set; }
        public string OS { get; private set; }
        public string OSVersion { get; private set; }
        public string JoinType { get; private set; }
        public string Owner { get; private set; }
        public string UserPrincipalName { get; private set; }
        public string MDM { get; private set; }
        public bool Compliant { get; private set; }
        public List<EntraGroup> Groups { get; private set; }


        // Class instances will be stored in the collection in the database
        // Primary key: id
        public EntraDevice(Dictionary<string, object> deviceProperties, LiteDBHandler database)
            : base("id", deviceProperties, database) { }
    }
}
