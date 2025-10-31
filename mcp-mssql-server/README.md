# MCP MSSQL Server

A reusable, production-ready MSSQL Model Context Protocol (MCP) server implementation for exposing read-only database access through the MCP standard.

## Overview

This library provides a complete MCP server implementation that:
- ?? Exposes database schema through `schema.describe` tool
- ??? Lists available tools through `tools.list` method
- ?? Executes read-only SELECT queries via `sql.execute_readonly` tool
- ?? Enforces query-level security (rejects DML/DDL statements)
- ?? Tracks performance metrics and query execution time
- ?? Comprehensive logging with ILogger support
- ? Follows JSON-RPC 2.0 protocol standard

## Installation

Add to your project:

```bash
dotnet add package MCP.MSSQL.Server
```

Or via NuGet Package Manager:
```
Install-Package MCP.MSSQL.Server
```

## Quick Start

### 1. Register in Dependency Injection

```csharp
using MCP.MSSQL.Server;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Server
var connectionString = builder.Configuration.GetConnectionString("CrimeSolverReadOnly");
var queryTimeout = builder.Configuration.GetValue("MCP:QueryTimeoutSeconds", 30);
var maxRowLimit = builder.Configuration.GetValue("MCP:MaxRowLimit", 1000);

builder.Services.AddSingleton(sp =>
    new MSSQLMCPServer(
        connectionString,
      queryTimeout,
        maxRowLimit,
        sp.GetRequiredService<ILogger<MSSQLMCPServer>>()));

var app = builder.Build();
```

### 2. Configure Application Settings

Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CrimeSolverReadOnly": "Server=yourserver;Database=yourdb;User Id=readonly_user;Password=***;Encrypt=true;TrustServerCertificate=false;"
  },
  "MCP": {
    "QueryTimeoutSeconds": 30,
  "MaxRowLimit": 1000
  }
}
```

### 3. Expose MCP Endpoints

```csharp
// MCP Invoke Endpoint
app.MapPost("/mcp/invoke", async (MCPRequest request, MSSQLMCPServer server) =>
{
    var response = await server.ProcessRequestAsync(request);
    return Results.Json(response);
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

## Usage Examples

### Discover Available Tools

```bash
curl -X POST https://localhost:5000/mcp/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "1",
    "method": "tools.list",
    "params": {}
  }'
```

### Retrieve Database Schema

```bash
curl -X POST https://localhost:5000/mcp/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
 "id": "2",
    "method": "schema.describe",
  "params": {}
  }'
```

### Execute a Read-Only Query

```bash
curl -X POST https://localhost:5000/mcp/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": "3",
    "method": "sql.execute_readonly",
    "params": {
   "query": "SELECT TOP 10 * FROM Cases WHERE CaseStatus = '\''Active'\''"
    }
  }'
```

## API Reference

### MCP Methods

#### tools.list
Advertises available database tools.

**Response:**
```json
{
  "tools": [
    {
      "name": "schema.describe",
      "description": "Returns database schema information...",
      "inputSchema": []
    },
    {
      "name": "sql.execute_readonly",
   "description": "Executes read-only SELECT queries...",
      "inputSchema": [...]
    }
  ]
}
```

#### schema.describe
Retrieves complete database schema including tables, columns, and relationships.

**Response:**
```json
{
  "databaseName": "YourDatabase",
"retrievedAt": "2024-01-01T12:00:00Z",
  "tables": [
    {
      "tableName": "Cases",
  "columns": [...],
      "foreignKeys": [...],
      "rowCount": 1000
    }
  ],
  "summary": "Retrieved schema for 10 tables..."
}
```

#### sql.execute_readonly
Executes a read-only SELECT query.

**Parameters:**
- `query` (string, required): The SELECT query to execute

**Response:**
```json
{
  "success": true,
  "query": "SELECT * FROM Cases",
  "rowCount": 100,
  "maxRowLimit": 1000,
  "isTruncated": false,
  "rows": [...],
  "columns": ["CaseID", "CaseName", ...],
  "executionTimeMs": 45,
  "summary": "Query returned 100 row(s)..."
}
```

## Security Features

- ? **Read-Only Enforcement**: Rejects INSERT, UPDATE, DELETE, CREATE, DROP, ALTER, EXEC, GRANT, REVOKE
- ? **Query Validation**: Validates all queries before execution
- ? **Connection-Level Security**: Uses dedicated read-only database user
- ? **Timeout Protection**: Configurable query timeout (default: 30 seconds)
- ? **Row Limits**: Configurable max rows returned (default: 1,000)
- ? **Comprehensive Logging**: All operations logged with execution metrics

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `ConnectionString` | - | SQL Server connection string (must use read-only user) |
| `QueryTimeoutSeconds` | 30 | Maximum query execution time in seconds |
| `MaxRowLimit` | 1000 | Maximum rows returned per query |

## Error Handling

The server returns JSON-RPC 2.0 error responses:

```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "error": {
    "code": -32602,
    "message": "Only SELECT queries are allowed. DML/DDL statements are not permitted."
  }
}
```

### Error Codes

- `-32601`: Method not found
- `-32602`: Invalid parameters
- `-32603`: Internal server error

## Best Practices

1. **Use a Dedicated Read-Only Database User**: Create a SQL Server user with SELECT-only permissions
2. **Enable Encryption**: Set `Encrypt=true` in connection string
3. **Monitor Logs**: Enable `Debug` logging for `MCP.MSSQL.Server` namespace
4. **Set Appropriate Timeouts**: Adjust `QueryTimeoutSeconds` based on your query patterns
5. **Adjust Row Limits**: Set `MaxRowLimit` based on client capabilities and network conditions

## License

MIT

## Support

For issues, feature requests, or contributions, please visit the repository.
