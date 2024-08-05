using System;
using System.Collections.Generic;
using System.Linq;


namespace Maestro
{
    public class MSGraphUrl
    {
        private string baseUrl;
        private Dictionary<string, string> parameters;

        public MSGraphUrl(string baseUrl)
        {
            this.baseUrl = baseUrl;
            this.parameters = new Dictionary<string, string>();
        }

        public MSGraphUrl AddFilter(string filter)
        {
            parameters["$filter"] = filter;
            return this;
        }

        public MSGraphUrl AddSearch(string search)
        {
            parameters["$search"] = search;
            return this;
        }

        public MSGraphUrl AddSelect(params string[] properties)
        {
            parameters["$select"] = string.Join(",", properties);
            return this;
        }

        public MSGraphUrl AddCount()
        {
            parameters["$count"] = "true";
            return this;
        }

        public MSGraphUrl AddExpand(params string[] expand)
        {
            parameters["$expand"] = string.Join(",", expand);
            return this;
        }

        public MSGraphUrl AddTop(int top)
        {
            parameters["$top"] = top.ToString();
            return this;
        }

        public MSGraphUrl AddOrderBy(params string[] properties)
        {
            parameters["$orderby"] = string.Join(",", properties);
            return this;
        }

        public string BuildFilterString(List<(string LogicalOperator, string Key, string ComparisonOperator, string Value)> filters)
        {
            if (filters == null || !filters.Any())
                return string.Empty;

            var filterParts = new List<string>();

            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                string filterPart = $"{filter.Key} {filter.ComparisonOperator} {filter.Value}";

                if (i > 0)
                {
                    filterPart = $"{filter.LogicalOperator} {filterPart}";
                }

                filterParts.Add(filterPart);
            }

            return string.Join(" ", filterParts);
        }
        public string Build()
        {
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            // Replace spaces and single quotes with their URL-encoded equivalents
            queryString = queryString
                .Replace(" ", "%20")
                .Replace("'", "%27");
            return $"{baseUrl}?{queryString}";
        }
    }
}
