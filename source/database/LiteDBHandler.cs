using LiteDB;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maestro
{
    public class LiteDBHandler
    {
        public readonly LiteDatabase Database;

        private LiteDBHandler(string databasePath)
        {
            Database = new LiteDatabase(databasePath);
        }

        public static LiteDBHandler CreateOrOpen(string databasePath)
        {
            try
            {
                return new LiteDBHandler(databasePath);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException ||
                ex is LiteException || ex is IOException)
            {
                Logger.Error($"Failed to open {databasePath}: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            Database.Dispose();
        }

        public IEnumerable<BsonDocument> FindInCollection<T>(string propertyName = "", BsonValue propertyValue = null)
        {
            var collection = Database.GetCollection<BsonDocument>(typeof(T).Name);
            if (string.IsNullOrEmpty(propertyName))
            {
                return collection.FindAll();
            }
            var query = LiteDB.Query.EQ(propertyName, propertyValue);
            return collection.Find(query);
        }

        public BsonDocument FindByPrimaryKey<T>(string primaryKeyValue)
        {
            return Database.GetCollection<BsonDocument>(typeof(T).Name).FindById(new BsonValue(primaryKeyValue));
        }

        public string FindValidAccessToken(string scope = "")
        {
            string bearerToken = "";
            var collection = Database.GetCollection<BsonDocument>("AccessToken");

            // Define current Unix timestamp
            var now = DateTime.UtcNow;
            var nowUnixTimestamp = DateTimeHandler.ConvertToUnixTimestamp(DateTime.UtcNow);

            // Use a single query to filter documents and find the matching JWT
            var farthestExpiryAccessToken = collection.FindAll()
                .Where(doc =>
                    doc.ContainsKey("NotBefore") &&
                    doc.ContainsKey("Expiry") &&
                    // Check if the JWT is currently valid
                    doc["NotBefore"].AsDateTime <= now && doc["Expiry"].AsDateTime >= now &&
                    // Check if the JWT has the required scope, if provided
                    (string.IsNullOrEmpty(scope) ||
                        (doc.ContainsKey("Scope") &&
                        doc["Scope"].IsString &&
                        doc["Scope"].AsString.Split(' ').Contains(scope))))
                // Find the JWT with the farthest expiration date
                .OrderByDescending(doc => doc["Expiry"].AsDateTime)
                .FirstOrDefault();

            if (farthestExpiryAccessToken != null)
            {
                Logger.Info($"Found valid access token with the required scope {scope} in the database");
                bearerToken = farthestExpiryAccessToken["Value"];
                Logger.VerboseTextOnly(bearerToken);
            }
            else
            {
                Logger.Info($"No valid JWTs with the required scope {scope} found in the database");
            }
            return bearerToken;
        }

        public string FindValidJwt(string scope = "")
        {
            string bearerToken = "";
            // Get the Jwt collection (or create, if doesn't exist)
            var collection = Database.GetCollection<BsonDocument>("Jwt");

            // Define current Unix timestamp
            var nowUnixTimestamp = DateTimeHandler.ConvertToUnixTimestamp(DateTime.UtcNow);

            // Use a single query to filter documents and find the matching JWT
            var farthestExpJwt = collection.FindAll()
                .Where(doc =>
                    doc.ContainsKey("nbf") &&
                    doc.ContainsKey("exp") &&
                    doc["nbf"].IsInt32 &&
                    doc["exp"].IsInt32 &&
                    // Check if the JWT is currently valid
                    doc["nbf"].AsInt32 <= nowUnixTimestamp && doc["exp"].AsInt32 >= nowUnixTimestamp &&
                    // Check if the JWT has the required scope, if provided
                    (string.IsNullOrEmpty(scope) ||
                        (doc.ContainsKey("scp") &&
                        doc["scp"].IsString &&
                        doc["scp"].AsString.Split(' ').Contains(scope))))
                // Find the JWT with the farthest expiration date
                .OrderByDescending(doc => doc["exp"].AsInt32)
                .FirstOrDefault();

            if (farthestExpJwt != null)
            {
                Logger.Info($"Found JWT with the required scope in the database");
                bearerToken = farthestExpJwt["bearerToken"];
                Logger.VerboseTextOnly(bearerToken);
            }
            else
            {
                Logger.Info("No JWTs with the required scope found in the database");
            }
            return bearerToken;
        }

        public string FindValidOAuthToken()
        {
            string refreshToken = "";
            // Get the Jwt collection (or create, if doesn't exist)
            var collection = Database.GetCollection<BsonDocument>("OAuthToken");

            // Define current Unix timestamp
            var nowUnixTimestamp = DateTimeHandler.ConvertToUnixTimestamp(DateTime.UtcNow);

            // Use a single query to filter documents and find the matching JWT
            var oAuthTokens = collection.FindAll();

            if (oAuthTokens != null)
            {
                Logger.Info($"Found OAuth token in the database");
                refreshToken = oAuthTokens.FirstOrDefault()["refreshToken"];
                Logger.DebugTextOnly(refreshToken);
            }
            else
            {
                Logger.Info("No OAuth tokens found in the database");
            }
            return refreshToken;
        }

        public ILiteCollection<T> GetCollection<T>(string typeName)
        {
            return Database.GetCollection<T>(typeName);
        }

        public QueryResult Query(string table, CommandLineOptions options)
        {
           return Query(table, options.Properties, options.Filter, options.OrderBy, options.Count, options.DryRun);
        }

        public QueryResult Query(string table, List<string> properties = null, string whereCondition = null, string orderBy = null, bool count = false, bool dryRun = false)
        {
            var collection = Database.GetCollection(table);
            var query = collection.Query();

            if (!string.IsNullOrWhiteSpace(whereCondition))
                query = query.Where(whereCondition);

            // Check if properties contains "ALL"
            bool returnAllProperties = properties != null && properties.Any(p => p.Equals("ALL", StringComparison.OrdinalIgnoreCase));

            if (properties != null && properties.Any() && !returnAllProperties)
            {
                string projection = string.Join(",", properties.Select(p => $"$.{p}"));
                query = (ILiteQueryable<BsonDocument>)query.Select(projection);
            }

            if (!string.IsNullOrWhiteSpace(orderBy))
                query = (ILiteQueryable<BsonDocument>)query.OrderBy(orderBy);

            if (dryRun)
                return new QueryResult(queryPlan: query.GetPlan());

            if (count)
                return new QueryResult(count: query.Count());

            return new QueryResult(documents: query.ToList());
        }

        // Upsert known objects with a primary key
        public bool Upsert<T>(T entity)
        {
            try
            {
                var collection = Database.GetCollection<T>(typeof(T).Name);
                collection.Upsert(entity);
                Logger.Verbose($"Upserted item in database: {typeof(T).Name}");
                return true;
            }
            catch (Exception ex) 
            {
                Logger.Error($"Failed to upsert item in database: {typeof(T).Name}");
                Logger.ExceptionDetails(ex);
                return false;
            }
        }
    }
}
