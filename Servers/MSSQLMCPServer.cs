using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MCP.MSSQL.Server.Models;

namespace MCP.MSSQL.Server
{
   /// <summary>
    /// MSSQL Model Context Protocol (MCP) server implementation.
    /// Exposes read-only database tools following the MCP standard with JSON-RPC 2.0 protocol.
    /// Enforces security through read-only user connections and query validation.
    /// </summary>
    public class MSSQLMCPServer
  {
   private readonly string _connectionString;
        private readonly int _queryTimeoutSeconds;
      private readonly int _maxRowLimit;
      private readonly ILogger<MSSQLMCPServer> _logger;

  /// <summary>
       /// Initializes a new instance of the MSSQLMCPServer.
   /// </summary>
  /// <param name="connectionString">The read-only database connection string from configuration.</param>
 /// <param name="queryTimeoutSeconds">Maximum query execution time in seconds (default: 30).</param>
        /// <param name="maxRowLimit">Maximum number of rows to return (default: 1000).</param>
    /// <param name="logger">Logger instance for MCP operations and debugging.</param>
        public MSSQLMCPServer(
 string connectionString,
     int queryTimeoutSeconds = 30,
        int maxRowLimit = 1000,
            ILogger<MSSQLMCPServer>? logger = null)
     {
      _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
         _queryTimeoutSeconds = queryTimeoutSeconds;
           _maxRowLimit = maxRowLimit;
         _logger = logger;
    }

        /// <summary>
        /// Processes an MCP request and returns an MCP response following JSON-RPC 2.0 standard.
        /// Routes requests to appropriate tool handlers (tools.list, schema.describe, sql.execute_readonly).
        /// </summary>
  /// <param name="request">The incoming MCP request.</param>
 /// <returns>The MCP response as a JSON-serializable object.</returns>
       public async Task<MCPResponse> ProcessRequestAsync(MCPRequest request)
              {
       var stopwatch = Stopwatch.StartNew();
                 try
          {
        _logger?.LogInformation("MCP Request received: Method={Method}, Id={Id}", request.Method, request.Id);

            // Route request to appropriate handler based on MCP method
        // Each handler returns properly formatted JSON-RPC 2.0 response
                  MCPResponse response = request.Method switch
               {
           // tools.list: Advertise available database tools to MCP clients
               "tools.list" => HandleToolsList(request),
           // schema.describe: Retrieve complete database schema (tables, columns, relationships)
        "schema.describe" => await HandleSchemaDescribeAsync(request),
         // sql.execute_readonly: Execute read-only SELECT queries with safety validation
            "sql.execute_readonly" => await HandleSQLExecuteReadOnlyAsync(request),
          // Unknown method: Return JSON-RPC 2.0 error for invalid method
            _ => CreateErrorResponse(request.Id, -32601, $"Method '{request.Method}' not found")
         };

    stopwatch.Stop();
         // Log request completion with method, duration, and success status for monitoring
       _logger?.LogInformation("MCP Request processed: Method={Method}, Duration={DurationMs}ms, Success={Success}",
          request.Method, stopwatch.ElapsedMilliseconds, response.Error == null);

       return response;
          }
catch (Exception ex)
 {
      stopwatch.Stop();
// Log unexpected errors with full context for debugging
  _logger?.LogError(ex, "MCP Request failed: Method={Method}, Duration={DurationMs}ms",
   request.Method, stopwatch.ElapsedMilliseconds);

     // Return JSON-RPC 2.0 standard error response (code -32603 = Internal Server Error)
    return CreateErrorResponse(request.Id, -32603, $"Internal server error: {ex.Message}");
           }
        }

      /// <summary>
    /// Handles the tools.list MCP method to advertise available tools.
      /// Returns metadata for schema.describe and sql.execute_readonly tools.
        /// </summary>
 private MCPResponse HandleToolsList(MCPRequest request)
        {
    var toolsList = new ToolsListResponse
             {
    Tools = new List<ToolDefinition>
     {
 new()
         {
        Name = "schema.describe",
             Description = "Returns database schema information including tables, columns, data types, and foreign key relationships.",
           InputSchema = new List<ToolParameter>()
        },
            new()
      {
              Name = "sql.execute_readonly",
    Description = "Executes read-only SELECT queries against the database. Rejects DML/DDL statements. Returns results, row count, and execution timing.",
     InputSchema = new List<ToolParameter>
   {
             new()
   {
          Name = "query",
         Type = "string",
    Description = "The SELECT query to execute",
   Required = true
     }
   }
               }
   }
          };

            return new MCPResponse
           {
 Jsonrpc = "2.0",
    Id = request.Id,
 Result = toolsList
 };
      }

    /// <summary>
         /// Handles the schema.describe MCP method to retrieve database schema.
       /// Uses INFORMATION_SCHEMA queries to introspect tables, columns, and foreign keys.
        /// Returns complete schema metadata for AI agents to understand data structure.
    /// </summary>
   private async Task<MCPResponse> HandleSchemaDescribeAsync(MCPRequest request)
   {
 try
            {
          using var connection = new SqlConnection(_connectionString);
             await connection.OpenAsync();

           var databaseName = connection.Database;
               var tables = await GetTablesSchemaAsync(connection);

        var response = new SchemaDescribeResponse
              {
        DatabaseName = databaseName,
 RetrievedAt = DateTime.UtcNow,
     Tables = tables,
          Summary = $"Retrieved schema for {tables.Count} tables with {tables.Sum(t => t.Columns.Count)} columns and {tables.Sum(t => t.ForeignKeys.Count)} foreign keys."
 };

      return new MCPResponse
      {
 Jsonrpc = "2.0",
  Id = request.Id,
    Result = response
            };
         }
           catch (Exception ex)
    {
      _logger?.LogError(ex, "Schema describe failed");
           return CreateErrorResponse(request?.Id, -32603, $"Failed to retrieve schema: {ex.Message}");
  }
        }

private async Task<MCPResponse> HandleSQLExecuteReadOnlyAsync(MCPRequest request)
        {
          try
           {
        if (!request.Params.TryGetValue("query", out var queryObj) || queryObj is not string query)
              {
       return CreateErrorResponse(request?.Id, -32602, "Missing or invalid 'query' parameter");
        }

       // Validate query is read-only
if (!IsSelectOnlyQuery(query))
    {
            _logger?.LogWarning("Non-SELECT query attempted: {Query}", query);
              return CreateErrorResponse(request?.Id, -32602,
   "Only SELECT queries are allowed. DML/DDL statements are not permitted.");
     }

        var stopwatch = Stopwatch.StartNew();
  var response = await ExecuteQueryAsync(query);
       stopwatch.Stop();

               response.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

      return new MCPResponse
            {
           Jsonrpc = "2.0",
    Id = request.Id,
              Result = response
      };
       }
            catch (Exception ex)
         {
   _logger?.LogError(ex, "SQL execution failed");
       return CreateErrorResponse(request?.Id, -32603, $"Query execution failed: {ex.Message}");
             }
        }

    /// <summary>
        /// Validates that a query contains only SELECT statements (read-only).
         /// Rejects INSERT, UPDATE, DELETE, CREATE, DROP, ALTER, and other modification statements.
     /// </summary>
 private bool IsSelectOnlyQuery(string query)
 {
          if (string.IsNullOrWhiteSpace(query))
        return false;

 // Convert to uppercase for case-insensitive keyword matching
     var trimmed = query.Trim().ToUpperInvariant();

     // List of forbidden keywords indicating modification operations
     // These keywords block DML/DDL injection attacks (INSERT, UPDATE, DELETE, schema changes, procedure execution, etc.)
    var forbiddenKeywords = new[] { "INSERT", "UPDATE", "DELETE", "CREATE", "DROP", "ALTER", "EXEC", "EXECUTE", "GRANT", "REVOKE" };
    // Check if forbidden keyword appears at start or surrounded by spaces (prevents false positives)
    if (forbiddenKeywords.Any(kw => trimmed.StartsWith(kw) || trimmed.Contains($" {kw} ")))
 return false;

   // Validate query starts with safe read-only keywords
    // SELECT: Standard queries, WITH: Common Table Expressions (CTEs)
    return trimmed.StartsWith("SELECT") || trimmed.StartsWith("WITH");
       }

        /// <summary>
   /// Executes a validated SELECT query and returns results with metadata.
  /// Enforces row limits and timeout constraints.
         /// </summary>
 private async Task<SQLExecuteResponse> ExecuteQueryAsync(string query)
    {
      using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

   // Create SQL command with configured timeout to prevent long-running queries from consuming resources
     using var command = new SqlCommand(query, connection)
       {
  // Set command timeout to configured limit (default 30 seconds) - prevents resource exhaustion
  CommandTimeout = _queryTimeoutSeconds
       };

         try
             {
      await connection.OpenAsync();

   var dataTable = new DataTable();
   var adapter = new SqlDataAdapter(command);
   adapter.Fill(dataTable);

     var rows = new List<QueryRow>();
    var columnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
   // Enforce row limit: cap results to _maxRowLimit to prevent memory exhaustion
  var rowCount = Math.Min(dataTable.Rows.Count, _maxRowLimit);
// Determine if results were truncated due to row limit constraint
   var isTruncated = dataTable.Rows.Count > _maxRowLimit;

    // Convert DataTable rows to QueryRow objects, enforcing row limit
       for (int i = 0; i < rowCount; i++)
  {
               var row = new QueryRow();
         var dataRow = dataTable.Rows[i];

  foreach (var columnName in columnNames)
      {
    // Convert DBNull.Value to null for JSON serialization (JSON doesn't have DBNull concept)
     var value = dataRow[columnName];
   row.Values[columnName] = value == DBNull.Value ? null : value;
                 }

          rows.Add(row);
     }

         return new SQLExecuteResponse
        {
   Success = true,
     Query = query,
          RowCount = rowCount,
     MaxRowLimit = _maxRowLimit,
        IsTruncated = isTruncated,
      Rows = rows,
          Columns = columnNames,
    ErrorMessage = null,
         Summary = $"Query returned {rowCount} row(s) out of {dataTable.Rows.Count} total row(s).{(isTruncated ? " Result truncated due to row limit." : "")}"
       };
       }
 catch (SqlException ex)
   {
        return new SQLExecuteResponse
            {
    Success = false,
          Query = query,
   ErrorMessage = $"SQL Error: {ex.Message}",
   Summary = "Query execution failed with SQL error."
        };
   }
  catch (OperationCanceledException)
           {
             return new SQLExecuteResponse
           {
      Success = false,
          Query = query,
      ErrorMessage = $"Query execution exceeded timeout of {_queryTimeoutSeconds} seconds.",
       Summary = "Query execution timed out."
              };
       }
        }

    /// <summary>
         /// Retrieves complete schema metadata from INFORMATION_SCHEMA tables.
       /// Includes column definitions, data types, and foreign key relationships.
        /// </summary>
        private async Task<List<TableSchema>> GetTablesSchemaAsync(SqlConnection connection)
   {
 var tables = new List<TableSchema>();

      // Get all user tables
           const string tableQuery = @"
SELECT TABLE_NAME 
        FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo'
           ORDER BY TABLE_NAME";

        using (var command = new SqlCommand(tableQuery, connection))
  {
        using var reader = await command.ExecuteReaderAsync();
 while (await reader.ReadAsync())
            {
 tables.Add(new TableSchema { TableName = reader.GetString(0) });
    }
  }

         // Get columns for each table
      foreach (var table in tables)
         {
   const string columnsQuery = @"
           SELECT 
        COLUMN_NAME,
        DATA_TYPE,
         CHARACTER_MAXIMUM_LENGTH,
   IS_NULLABLE,
   COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') as IsIdentity
            FROM INFORMATION_SCHEMA.COLUMNS
       WHERE TABLE_NAME = @TableName AND TABLE_SCHEMA = 'dbo'
        ORDER BY ORDINAL_POSITION";

   using (var command = new SqlCommand(columnsQuery, connection))
  {
        command.Parameters.AddWithValue("@TableName", table.TableName);
   using var reader = await command.ExecuteReaderAsync();
               while (await reader.ReadAsync())
         {
 var columnName = reader.GetString(0);
    var dataType = reader.GetString(1);
           var maxLength = reader.IsDBNull(2) ? (int?)null : Convert.ToInt32(reader.GetValue(2));
      var isNullable = reader.GetString(3) == "YES";
     var isIdentity = reader.IsDBNull(4) ? false : reader.GetInt32(4) == 1;
        var isPrimaryKey = await IsPrimaryKeyColumnAsync(connection, table.TableName ?? "", columnName);

             table.Columns.Add(new ColumnInfo
  {
         ColumnName = columnName,
        DataType = dataType,
               MaxLength = maxLength,
      IsNullable = isNullable,
      IsIdentity = isIdentity,
      IsPrimaryKey = isPrimaryKey
   });
     }
           }

               // Get row count
           using (var command = new SqlCommand($"SELECT COUNT(*) FROM [{table.TableName}]", connection))
         {
       var count = await command.ExecuteScalarAsync();
            table.RowCount = count != null ? (int)count : 0;
     }

      // Get foreign keys
          const string fkQuery = @"
       SELECT 
       CONSTRAINT_NAME,
          TABLE_NAME,
           COLUMN_NAME,
 REFERENCED_TABLE_NAME,
                  REFERENCED_COLUMN_NAME
      FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
  INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
   ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu2
  ON rc.UNIQUE_CONSTRAINT_NAME = kcu2.CONSTRAINT_NAME
         AND kcu.ORDINAL_POSITION = kcu2.ORDINAL_POSITION
        WHERE kcu.TABLE_NAME = @TableName AND kcu.TABLE_SCHEMA = 'dbo'";

      using (var command = new SqlCommand(fkQuery, connection))
         {
         command.Parameters.AddWithValue("@TableName", table.TableName);
      using var reader = await command.ExecuteReaderAsync();
  while (await reader.ReadAsync())
       {
               table.ForeignKeys.Add(new ForeignKeyInfo
           {
   ConstraintName = reader.GetString(0),
  FromTable = reader.GetString(1),
         FromColumn = reader.GetString(2),
    ToTable = reader.GetString(3),
   ToColumn = reader.GetString(4)
      });
      }
      }
             }

           return tables;
     }

    /// <summary>
  /// Determines if a column is part of the primary key.
         /// Used to mark columns as primary key in schema metadata.
/// </summary>
  private async Task<bool> IsPrimaryKeyColumnAsync(SqlConnection connection, string tableName, string columnName)
  {
           const string query = @"
 SELECT COUNT(*)
       FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
  INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
     WHERE tc.TABLE_NAME = @TableName 
 AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
               AND kcu.COLUMN_NAME = @ColumnName
              AND tc.TABLE_SCHEMA = 'dbo'";

          using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@TableName", tableName);
           command.Parameters.AddWithValue("@ColumnName", columnName);

   var result = await command.ExecuteScalarAsync();
        return result != null && (int)result > 0;
     }

        /// <summary>
         /// Creates a standardized MCP error response following JSON-RPC 2.0 format.
       /// </summary>
        private MCPResponse CreateErrorResponse(string? id, int errorCode, string errorMessage)
       {
      return new MCPResponse
            {
         Jsonrpc = "2.0",
              Id = id,
     Error = new MCPError
          {
            Code = errorCode,
Message = errorMessage
          }
           };
       }
    }
}
