using LiteDB;
using System.Collections.Generic;
using System.Linq;

namespace Maestro
{
    public static class BsonDocumentHandler
    {
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

        public static Dictionary<string, object> ToDictionary(BsonDocument doc)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var element in doc)
            {
                dictionary[element.Key] = ConvertBsonValue(element.Value);
            }
            return dictionary;
        }
    }
}
