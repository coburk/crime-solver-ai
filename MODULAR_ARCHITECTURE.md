# CrimeSolverAI MCP - Modular Architecture

## Overview

This repository contains a modular, production-ready architecture with two separate .NET 9 projects:

```
CrimeSolverAI/
??? mcp-mssql-server/ - Reusable MCP Library
?   ??? Models/  - MCPRequest, MCPResponse, etc.
?   ??? Servers/ - MSSQLMCPServer implementation
?   ??? GlobalUsings.cs
?   ??? mcp-mssql-server.csproj    - Library project file
?   ??? README.md     - Library documentation
?
??? Tests/     - Test Projects
?   ??? MCPServerTests.cs - Integration tests
?   ??? MCPClientServiceTests.cs   - Model tests
?   ??? QUICK_START.md
?
??? Program.cs    - CrimeSolverAI Host Application
??? appsettings.json        - Configuration
??? CrimeSolverAI.csproj       - References mcp-mssql-server library
??? CrimeSolverAI.sln      - Solution file
```

## Architecture Benefits

### 1. REUSABILITY
The `mcp-mssql-server` library can be packaged as a NuGet package and used in other projects:
```bash
dotnet add package MCP.MSSQL.Server
```

### 2. SEPARATION OF CONCERNS
- Library (mcp-mssql-server): Core MCP protocol implementation
- Application (CrimeSolverAI): Domain-specific logic and endpoints

### 3. INDEPENDENT TESTING
- Library can be tested in isolation
- Application-level tests focus on business logic

### 4. EASY MAINTENANCE
- Security updates to MCP don't require full application release
- Can version library independently

## Project Structure

### mcp-mssql-server (Library)

**Namespace:** MCP.MSSQL.Server

```csharp
using MCP.MSSQL.Server;
using MCP.MSSQL.Server.Models;

// Models
- MCPRequest / MCPResponse / MCPError
- ToolDefinition / ToolParameter / ToolsListResponse
- SchemaDescribeResponse / TableSchema / ColumnInfo / ForeignKeyInfo
- SQLExecuteResponse / QueryRow

// Servers
- MSSQLMCPServer
```

**Purpose:** Provides a complete, reusable MCP server implementation for SQL databases.

### CrimeSolverAI (Application)

**Namespace:** CrimeSolverAI

**Dependencies:** References `mcp-mssql-server` library

```csharp
using MCP.MSSQL.Server;
using MCP.MSSQL.Server.Models;
```

**Purpose:** Hosts the MCP server in an ASP.NET Core web application specific to the crime-solving domain.

## Building the Solution

### BUILD EVERYTHING
```bash
dotnet build CrimeSolverAI.sln
```

### BUILD JUST THE LIBRARY
```bash
dotnet build mcp-mssql-server/mcp-mssql-server.csproj
```

### BUILD JUST THE APPLICATION
```bash
dotnet build CrimeSolverAI.csproj
```

### RUN TESTS
```bash
dotnet test CrimeSolverAI.sln
```

## Publishing the Library

To publish `mcp-mssql-server` as a NuGet package:

```bash
cd mcp-mssql-server
dotnet pack -c Release
```

This generates `MCP.MSSQL.Server.*.nupkg` which can be published to NuGet.org or a private feed.

## Getting Started

### 1. Update Configuration
Edit `appsettings.json` with your database credentials:
```json
{
  "ConnectionStrings": {
    "CrimeSolverReadOnly": "Server=localhost;Database=SequelCityCrimesDB;..."
  }
}
```

### 2. Run the Application
```bash
dotnet run
```

Opens browser to: https://localhost:61087

### 3. Test with curl
```bash
curl -X POST https://localhost:61087/mcp/invoke \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":"1","method":"tools.list","params":{}}'
```

## Using the Library in Other Projects

### CREATE A NEW ASP.NET CORE PROJECT
```bash
dotnet new webapi -n MyMCPClient
```

### ADD REFERENCE TO MCP-MSSQL-SERVER
```bash
cd MyMCPClient
dotnet add reference ../mcp-mssql-server/mcp-mssql-server.csproj
```

### USE IN PROGRAM.CS
```csharp
using MCP.MSSQL.Server;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Server
builder.Services.AddSingleton(sp =>
    new MSSQLMCPServer(
        connectionString,
    queryTimeout,
        maxRowLimit,
  sp.GetRequiredService<ILogger<MSSQLMCPServer>>()));

var app = builder.Build();

app.MapPost("/mcp/invoke", async (MCPRequest request, MSSQLMCPServer server) =>
{
    var response = await server.ProcessRequestAsync(request);
    return Results.Json(response);
});

app.Run();
```

## Repository Structure Guidelines

### DO'S
- Keep `mcp-mssql-server` folder for library-only code
- Reference library via `ProjectReference` in CrimeSolverAI.csproj
- Use library namespaces (MCP.MSSQL.Server.*)
- Share library via NuGet

### DON'TS
- Don't add domain-specific code to the library
- Don't add web controllers to the library
- Don't duplicate library code

## Development Workflow

1. MAKE CHANGES TO LIBRARY CODE (mcp-mssql-server/)
   - Increment version in csproj
   - Build and test locally
   - Commit to main

2. MAKE CHANGES TO APPLICATION (CrimeSolverAI/)
   - Update application-specific logic
   - Commit to main

3. PUBLISH LIBRARY
   ```bash
   dotnet pack mcp-mssql-server/ -c Release
   dotnet nuget push bin/Release/*.nupkg -s https://api.nuget.org/v3/index.json
   ```

4. UPDATE APPLICATION TO USE LATEST LIBRARY (if published to NuGet)
   ```bash
   dotnet add package MCP.MSSQL.Server --version X.Y.Z
 ```

## File Organization

```
??? mcp-mssql-server/
?   ??? bin/  (build output - git ignore)
?   ??? obj/    (build output - git ignore)
?   ??? Models/
?   ?   ??? MCPRequest.cs
?   ?   ??? ToolDefinition.cs
?   ?   ??? SchemaDescribeResponse.cs
?   ?   ??? SQLExecuteResponse.cs
?   ??? Servers/
?   ?   ??? MSSQLMCPServer.cs
?   ??? GlobalUsings.cs
?   ??? mcp-mssql-server.csproj
?   ??? README.md
?
??? Tests/
?   ??? MCPServerTests.cs     (integration tests)
?   ??? MCPClientServiceTests.cs (model tests)
?   ??? QUICK_START.md
?   ??? MANUAL_TESTING_GUIDE.md
?
??? Program.cs        (CrimeSolverAI entry point)
??? appsettings.json
??? CrimeSolverAI.csproj
??? CrimeSolverAI.sln
??? README.md
??? .gitignore
??? GlobalUsings.cs
```

## Next Steps

1. [COMPLETE] - Modular architecture with library and application
2. [NEXT] - Create separate GitHub repository for `mcp-mssql-server`
3. [NEXT] - Publish `MCP.MSSQL.Server` NuGet package
4. [NEXT] - Add CI/CD pipeline (GitHub Actions)
5. [NEXT] - Add more MCP tools and capabilities

## Support & Contributions

For the library: Visit https://github.com/coburk/mcp-mssql-server

For the application: Visit https://github.com/coburk/crime-solver-ai

---

Version: 1.0.0
Last Updated: 2024
License: MIT
