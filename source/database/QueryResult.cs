using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Maestro
{
    public class QueryResult
    {
        public List<Dictionary<string, object>> Documents { get; set; }
        public int? Count { get; set; }
        public string QueryPlan { get; set; }

        public QueryResult(List<BsonDocument> documents = null, int? count = null, BsonDocument queryPlan = null)
        {
            string queryPlanString = string.Empty;
            if (queryPlan != null)
            {
                queryPlanString = queryPlan.ToString();
            }
            if (documents != null)
            {
                Documents = documents.Select(doc => ToDictionary(doc)).ToList();
            }
            Count = count;
            QueryPlan = queryPlanString;
        }

        public string ToJson(bool raw)
        {
            Formatting formatting = raw ? Formatting.None : Formatting.Indented;
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = formatting,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            };

            // For dry run, just return the query plan as a string
            if (!string.IsNullOrEmpty(QueryPlan))
            {
                return JsonHandler.GetProperties(QueryPlan, raw, null, false);
            }

            // For count query, just return the count as a string
            else if (Count.HasValue)
            {
                return Count.Value.ToString(); 
            }

            // For document query, serialize just the documents
            else
            {
                return JsonConvert.SerializeObject(Documents, settings); 
            }
        }

        private static Dictionary<string, object> ToDictionary(BsonDocument doc)
        {
            return doc.ToDictionary(x => x.Key, x => ConvertBsonValue(x.Value));
        }

        private static object ConvertBsonValue(BsonValue bsonValue)
        {
            if (bsonValue.IsInt32) return bsonValue.AsInt32;
            if (bsonValue.IsInt64) return bsonValue.AsInt64;
            if (bsonValue.IsDouble) return bsonValue.AsDouble;
            if (bsonValue.IsString) return bsonValue.AsString;
            if (bsonValue.IsBoolean) return bsonValue.AsBoolean;
            if (bsonValue.IsDateTime) return bsonValue.AsDateTime;
            if (bsonValue.IsArray) return bsonValue.AsArray.Select(ConvertBsonValue).ToList();
            if (bsonValue.IsDocument) return ToDictionary(bsonValue.AsDocument);
            return bsonValue.RawValue;
        }
    }
}
