using Maestro;
using Newtonsoft.Json;
using System.Collections.Generic;

public class MSGraphBatchRequest
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("headers")]
    public Dictionary<string, string> Headers { get; set; }

    [JsonProperty("body")]
    public object Body { get; set; }

    public MSGraphBatchRequest(string id, string method, string baseUrl, string endpoint, Dictionary<string, string> queryParams = null)
    {
        var urlBuilder = new MSGraphUrl(baseUrl);
        if (queryParams != null)
        {
            if (queryParams.ContainsKey("count"))
            {
                urlBuilder.AddCount();
            }
            if (queryParams.ContainsKey("expand"))
            {
                urlBuilder.AddExpand(queryParams["expand"]);
            }
            if (queryParams.ContainsKey("filter"))
            {
                urlBuilder.AddFilter(queryParams["filter"]);
            }
            if (queryParams.ContainsKey("orderBy"))
            {
                urlBuilder.AddOrderBy(queryParams["orderBy"]);
            }
            if (queryParams.ContainsKey("search"))
            {
                urlBuilder.AddSearch(queryParams["search"]);
            }
            if (queryParams.ContainsKey("select"))
            {
                urlBuilder.AddSelect(queryParams["select"]);
            }
            if (queryParams.ContainsKey("top"))
            {

            }
        }

        Id = id;
        Method = method;
        Url = baseUrl + endpoint;
        Headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        };
    }
}