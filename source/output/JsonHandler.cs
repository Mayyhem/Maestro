using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Maestro
{
    internal static class JsonHandler
    {
        public static string PrintProperties(string jsonBlob, string[] properties = null)
        {
            // Parse the JSON blob
            JToken parsedJson = JToken.Parse(jsonBlob);

            if (parsedJson is JArray jsonArray)
            {
                // Handle JSON array
                JArray filteredArray = new JArray();
                foreach (JObject obj in jsonArray)
                {
                    filteredArray.Add(FilterProperties(obj, properties));
                }
                string prettyPrintedJson = filteredArray.ToString(Formatting.Indented);
                Console.WriteLine(prettyPrintedJson);
                return prettyPrintedJson;
            }
            else if (parsedJson is JObject jsonObject)
            {
                // Handle JSON object
                JObject filteredObject = FilterProperties(jsonObject, properties);
                string prettyPrintedJson = filteredObject.ToString(Formatting.Indented);
                Console.WriteLine(prettyPrintedJson);
                return prettyPrintedJson;
            }

            return null;
        }

        private static JObject FilterProperties(JObject jsonObject, string[] properties = null)
        {
            JObject selectedElements = new JObject();

            // Determine the primary key value if it exists
            string primaryKey = jsonObject["primaryKey"]?.ToString();

            // Always include the primary key and display it first
            if (!string.IsNullOrEmpty(primaryKey) && jsonObject[primaryKey] != null)
            {
                selectedElements[primaryKey] = jsonObject[primaryKey];
            }

            // No specific properties provided, print all properties
            if (properties == null || properties.Length == 0 || properties.Contains("ALL", StringComparer.OrdinalIgnoreCase))
            {
                foreach (var property in jsonObject.Properties())
                {
                    if (property.Name != "_id" && property.Name != "primaryKey" && property.Name != primaryKey)
                    {
                        selectedElements[property.Name] = property.Value;
                    }
                }
            }
            // Print specified properties and primary key only
            else
            {
                foreach (var prop in properties)
                {
                    if (jsonObject[prop] != null && prop != "_id" && prop != "primaryKey" && prop != primaryKey)
                    {
                        selectedElements[prop] = jsonObject[prop];
                    }
                }
            }

            return selectedElements;
        }
    }
}
