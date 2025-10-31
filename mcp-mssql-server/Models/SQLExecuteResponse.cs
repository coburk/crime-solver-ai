namespace MCP.MSSQL.Server.Models
{
    public class QueryRow
    {
  public Dictionary<string, object?> Values { get; set; } = new();
    }

    public class SQLExecuteResponse
    {
     public bool Success { get; set; }
      public string? Query { get; set; }
     public int RowCount { get; set; }
 public int MaxRowLimit { get; set; }
     public bool IsTruncated { get; set; }
  public List<QueryRow> Rows { get; set; } = new();
      public List<string> Columns { get; set; } = new();
     public long ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
     public string? Summary { get; set; }
    }
}
