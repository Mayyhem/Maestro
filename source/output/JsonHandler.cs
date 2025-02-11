using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

//using System.Xml;

namespace Maestro
{
    internal static class JsonHandler
    {
        public static string GetProperties(JArray jsonArray, bool raw = false, string[] properties = null, bool print = true)
        {
            // Handle JSON array
            JArray filteredArray = new JArray();
            foreach (JObject obj in jsonArray)
            {
                filteredArray.Add(FilterProperties(obj, properties));
            }

            // Don't print empty results
            if (filteredArray.Count == 0)
            {
                return null;
            }

            string formattedJson = filteredArray.ToString(raw ? Formatting.None : Formatting.Indented);
            if (print)
            {
                Logger.InfoTextOnly(formattedJson);
            }
            return formattedJson;
        }

        public static string GetProperties(JObject jsonObject, bool raw = false, string[] properties = null, bool print = true)
        {
            // Handle JSON object
            JObject filteredObject = FilterProperties(jsonObject, properties);

            // Don't print empty results
            if (filteredObject.Count == 0)
            {
                return null;
            }
            string formattedJson = filteredObject.ToString(raw ? Formatting.None : Formatting.Indented);
            if (print)
            {
                Logger.InfoTextOnly(formattedJson);
            }
            return formattedJson;
        }

        public static string GetProperties(string jsonBlob, bool raw = false, string[] properties = null, bool print = true)
        {
            // Parse the JSON blob
            try
            {
                JToken parsedJson = JToken.Parse(jsonBlob);

                if (parsedJson is JArray jsonArray)
                {
                    // Handle JArray
                    return GetProperties(jsonArray, raw, properties, print);
                }

                if (parsedJson is JObject jsonObject)
                {
                    // Handle JObject
                    return GetProperties(jsonObject, raw, properties, print);
                }
            }
            catch
            {
                Logger.Error("Could not parse message as JSON");
                Logger.DebugTextOnly(jsonBlob);
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

            // Create a case-insensitive HashSet for property matching
            var propertySet = properties != null
                ? new HashSet<string>(properties, StringComparer.OrdinalIgnoreCase)
                : null;

            bool includeAll = propertySet == null || propertySet.Count == 0 || propertySet.Contains("ALL", StringComparer.OrdinalIgnoreCase);

            foreach (var property in jsonObject.Properties())
            {
                if (property.Name == "_id" || property.Name == "primaryKey" || property.Name == primaryKey)
                {
                    continue; // Skip these special properties
                }

                if (includeAll || (propertySet != null && propertySet.Contains(property.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    selectedElements[property.Name] = property.Value;
                }
            }

            return selectedElements;
        }
    }
}
