﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Maestro
{ 
    public class EntraGroup : JsonObject
    {
        // Class instances will be stored in the collection in the database
        // Primary key: id
        public EntraGroup(Dictionary<string, object> properties, LiteDBHandler database) 
            : base("id", properties, database) { 
        }
    }
}
