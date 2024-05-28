using System.Collections.Generic;

namespace Maestro
{
    public class AccessToken : JsonObject
    {
        /*
        public string EncodedValue { get; }
        public string aud { get; set; } // Audience
        public string amr { get; set; } // Authentication Method Reference
        public string app_displayname { get; set; } // Application Display Name
        public string appid { get; set; } // Application ID
        public string appidacr { get; set; } // Application ID Authentication Context Class Reference
        public string deviceid { get; set; } // Device ID
        public DateTime exp { get; set; } // Expiration Time
        public string family_name { get; set; } // Family Name
        public string given_name { get; set; } // Given Name
        public DateTime iat { get; set; } // Issued At
        public string idtyp { get; set; } // ID Type
        public string ipaddr { get; set; } // IP Address
        public string name { get; set; } // Name
        public DateTime nbf { get; set; } // Not Before
        public string nonce { get; set; } // Nonce
        public string oid { get; set; } // Object IDken(string encodedValue)
        {
            EncodedValue = encodedValue;
        }
        public string[] scp { get; set; } // Scope
        public string[] signin_state { get; set; } // Sign-In State
        public string tid { get; set; } // Tenant ID
        public string unique_name { get; set; } // Unique Name
        public string upn { get; set; } // User Principal Name

        protected AccessTo
        */

        // Set primary key
        public AccessToken(Dictionary<string, object> tokenObject) : base("oid", tokenObject) { }
    }
}