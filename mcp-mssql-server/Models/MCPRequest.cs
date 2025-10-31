namespace MCP.MSSQL.Server.Models
{
    public class MCPRequest
    {
 [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
        public string? Id { get; set; }

   [JsonPropertyName("method")]
  public string? Method { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, object> Params { get; set; } = new();
    }

    public class MCPResponse
 {
  [JsonPropertyName("jsonrpc")]
        public string Jsonrpc { get; set; } = "2.0";

   [JsonPropertyName("id")]
        public string? Id { get; set; }

 [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
   public object? Result { get; set; }

   [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MCPError? Error { get; set; }
    }

public class MCPError
    {
      [JsonPropertyName("code")]
        public int Code { get; set; }

   [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
    }
}
