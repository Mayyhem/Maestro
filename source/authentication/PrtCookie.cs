using System;

namespace Maestro
{
    public class PrtCookie : Credential
    {
        public bool ExpiryIsEstimated { get; private set; }
        public Jwt Jwt { get; private set; }
        public string Nonce { get; private set; }
        //public string DeviceName { get; private set; }
        //public string UserPrincipalName { get; private set; }

        public PrtCookie(string base64BearerToken, LiteDBHandler database)
            : base("PRT Cookie", base64BearerToken, database)
        {
            // Decode and parse properties of JWT and store in database
            Jwt = new Jwt("refresh_token", base64BearerToken, database);

            // Set the expiry to 35 minutes from the request timestamp or from now if not provided
            if (Jwt.Properties.ContainsKey("iat"))
            {
                string issuedAt = (string)Jwt.Properties["iat"];
                Requested = DateTimeHandler.ConvertFromUnixTimestamp(int.Parse(issuedAt));
            }
            else
            {
                Requested = DateTime.UtcNow;
            }
            Expiry = Requested.AddMinutes(35);

            // Always true for PRT cookies, useful to note in Credential table
            ExpiryIsEstimated = true;

            if (Jwt.Properties.ContainsKey("request_nonce"))
            {
                Nonce = (string)Jwt.Properties["request_nonce"];
            }

            if (database != null)
            {
                // Add to the PrtCookie table
                bool upsertSuccessful = database.Upsert(this);
                database.Upsert(this);
                if (!upsertSuccessful)
                {
                    Logger.Error($"Failed to upsert {GetType()} in database");
                }
                else
                {
                    Logger.Info($"Upserted {GetType()} in database");
                }

                // Add to Credential table
                upsertSuccessful = Upsert(database);
                if (!upsertSuccessful)
                {
                    Logger.Error($"Failed to upsert {GetType()} in Credential table");
                }
                else
                {
                    Logger.Info($"Upserted {GetType()} in Credential table");
                }
            }
        }
    } 
}
