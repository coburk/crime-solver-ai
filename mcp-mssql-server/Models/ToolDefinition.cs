namespace MCP.MSSQL.Server.Models
{
/// <summary>
    /// Represents a tool parameter definition for MCP tools.list response.
    /// </summary>
    public class ToolParameter
    {
   /// <summary>Gets or sets the parameter name.</summary>
        [JsonPropertyName("name")]
    public string? Name { get; set; }

        /// <summary>Gets or sets the parameter type (e.g., "string").</summary>
        [JsonPropertyName("type")]
      public string? Type { get; set; }

        /// <summary>Gets or sets a description of the parameter.</summary>
      [JsonPropertyName("description")]
      public string? Description { get; set; }

    /// <summary>Gets or sets whether this parameter is required.</summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; }
    }

    /// <summary>
    /// Represents an MCP tool definition returned by tools.list.
    /// Follows MCP protocol for tool discovery and capability advertisement.
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>Gets or sets the tool name (e.g., "schema.describe", "sql.execute_readonly").</summary>
        [JsonPropertyName("name")]
     public string? Name { get; set; }

   /// <summary>Gets or sets a description of what the tool does.</summary>
        [JsonPropertyName("description")]
     public string? Description { get; set; }

        /// <summary>Gets or sets the list of parameters this tool accepts.</summary>
        [JsonPropertyName("inputSchema")]
        public List<ToolParameter> InputSchema { get; set; } = new();
    }

 /// <summary>
    /// Response model for the tools.list MCP method.
    /// Advertises available tools and their capabilities to MCP clients.
    /// </summary>
    public class ToolsListResponse
    {
 /// <summary>Gets or sets the list of available tools.</summary>
        [JsonPropertyName("tools")]
        public List<ToolDefinition> Tools { get; set; } = new();
    }
}
