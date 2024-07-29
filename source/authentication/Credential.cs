using LiteDB;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Maestro
{
    public class Credential
    {
        // Hash of Type and Value are used as the primary key to ensure upserts don't create new rows
        [BsonId]
        public string CompositeKey { get; private set; }
        public DateTime Expiry { get; set; }
        public DateTime Requested { get; set; }
        public string TenantId { get; set; }
        public string Type { get; set; }
        public string Value { get; private set; }

        public Credential(string type, string value)
        {
            CompositeKey = GenerateCompositeKey(type, value);
            Type = type;
            Value = value;
        }

        private static string GenerateCompositeKey(string type, string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(type + value));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
