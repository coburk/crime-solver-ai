using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MCP.MSSQL.Server;
using MCP.MSSQL.Server.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddLogging();

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

app.UseHttpsRedirection();
app.UseAuthorization();

// Default home endpoint - displays the API status and available endpoints
app.MapGet("/", () => Results.Content(
    """
<!DOCTYPE html>
<html>
<head>
    <title>CrimeSolverAI - MCP Server</title>
    <style>
   body { font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }
      .container { max-width: 900px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
h1 { color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }
 h2 { color: #555; margin-top: 30px; }
  .endpoint { background: #f8f9fa; padding: 15px; margin: 10px 0; border-left: 4px solid #007bff; border-radius: 4px; font-family: monospace; }
      .status { color: #28a745; font-weight: bold; }
        .info { background: #e7f3ff; padding: 15px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #2196F3; }
   code { background: #f4f4f4; padding: 2px 6px; border-radius: 3px; }
        ul { line-height: 1.8; }
</style>
</head>
<body>
    <div class="container">
  <h1>🔍 CrimeSolverAI - MCP Server</h1>
        <p class="status">✓ Server is running and healthy</p>

     <div class="info">
  <strong>Model Context Protocol (MCP)</strong> implementation for read-only SQL database access.
   The server exposes database tools through standard JSON-RPC 2.0 endpoints.
        </div>
   
        <h2>Available Endpoints</h2>

        <div class="endpoint">
            <strong>POST /mcp/invoke</strong><br/>
   Processes MCP requests for database operations (tools.list, schema.describe, sql.execute_readonly)
        </div>
        
        <div class="endpoint">
    <strong>GET /health</strong><br/>
          Health check endpoint that returns server status
 </div>

        <h2>MCP Tools</h2>
        <ul>
    <li><strong>tools.list</strong> - Discover available database tools</li>
   <li><strong>schema.describe</strong> - Retrieve complete database schema with tables, columns, and relationships</li>
     <li><strong>sql.execute_readonly</strong> - Execute SELECT queries with safety constraints</li>
 </ul>
     
        <h2>Quick Start</h2>
    <p>Use the MCP Client to connect to this server, or make HTTP requests directly:</p>
        <div class="endpoint">
          curl -X POST https://localhost:61087/mcp/invoke \<br/>
            &nbsp;&nbsp;-H "Content-Type: application/json" \<br/>
      &nbsp;&nbsp;-d '{<br/>
            &nbsp;&nbsp;&nbsp;&nbsp;"jsonrpc": "2.0",<br/>
      &nbsp;&nbsp;&nbsp;&nbsp;"id": "1",<br/>
        &nbsp;&nbsp;&nbsp;&nbsp;"method": "tools.list",<br/>
   &nbsp;&nbsp;&nbsp;&nbsp;"params": {}<br/>
      &nbsp;&nbsp;}'
        </div>
     
        <h2>Configuration</h2>
    <ul>
            <li>Query Timeout: 30 seconds</li>
            <li>Max Row Limit: 1,000 rows</li>
   <li>Database: Read-only access via configured connection string</li>
      </ul>
        
        <p style="margin-top: 40px; color: #999; font-size: 12px;">
   MCP Library: <code>mcp-mssql-server</code> | Documentation: <a href="https://github.com/your-org/mcp-mssql-server" target="_blank">GitHub</a>
      </p>
    </div>
</body>
</html>
""", "text/html"));

// MCP Invoke Endpoint
app.MapPost("/mcp/invoke", async (MCPRequest request, MSSQLMCPServer server) =>
{
    var response = await server.ProcessRequestAsync(request);
    return Results.Json(response);
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();