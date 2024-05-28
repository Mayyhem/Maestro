using System.Collections.Generic;

namespace Maestro
{
    public class RefreshToken : JsonObject
    {
        /*
        public DateTime iat { get; set; } // Issued At
        public bool is_primary { get; set; } // Is Primary
        public string request_nonce { get; set; } // Request Nonce
        public string win_ver { get; set; } // Windows Version
        public string x_client_platform { get; set; } // X-Client-Platform
        */

        // Set primary key
        public RefreshToken(Dictionary<string, object> tokenObject) : base("x5c", tokenObject) { }
    }
}