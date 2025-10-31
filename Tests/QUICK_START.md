# MCP Quick Start Guide

## 1. Project Setup

### Add Required NuGet Packages
dotnet add package Microsoft.Data.SqlClient 
dotnet add package Microsoft.Extensions.Configuration 
dotnet add package Microsoft.Extensions.DependencyInjection 
dotnet add package Xunit 
dotnet add package Moq

### Update appsettings.json
{ "ConnectionStrings": { "CrimeSolverReadOnly": "Server=Burk7;Database=SequelCityCrimesDB;User Id=readonly_user;Password=YOUR_ACTUAL_PASSWORD;Encrypt=true;TrustServerCertificate=false;" }, "MCP": { "QueryTimeoutSeconds": 30, "MaxRowLimit": 1000, "ServerBaseUrl": "http://localhost:5000" }, "Logging": { "LogLevel": { "Default": "Information", "CrimeSolverAI.MCP": "Debug" } } }

## 2. Running the Server
From project root
dotnet run
Server will listen on http://localhost:5000
Endpoint: POST http://localhost:5000/mcp/invoke

## 3. Running Unit Tests
Run all tests
	dotnet test
Run specific test class
	dotnet test --filter ClassName=MCPServerTests
Run with verbose output
	dotnet test -v n

## 4. Running Integration Tests

Requires live database connection. Update connection string first:
dotnet test --filter ClassName=MCPServerTests

## 5. Manual Testing with curl
Test tools.list
	curl -X POST http://localhost:5000/mcp/invoke 
	-H "Content-Type: application/json" 
	-d '{"jsonrpc":"2.0","id":"1","method":"tools.list","params":{}}'

Test schema.describe
	curl -X POST http://localhost:5000/mcp/invoke 
	-H "Content-Type: application/json" 
	-d '{"jsonrpc":"2.0","id":"2","method":"schema.describe","params":{}}'

Test sql.execute_readonly
	curl -X POST http://localhost:5000/mcp/invoke 
	-H "Content-Type: application/json" 
	-d '{"jsonrpc":"2.0","id":"3","method":"sql.execute_readonly","params":{"query":"SELECT TOP 5 * FROM Cases"}}'

## 6. Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Connection refused | Ensure server is running on port 5000 |
| Database not found | Verify server=Burk7 and database=SequelCityCrimesDB |
| Login failed | Check readonly_user credentials in SQL Server |
| Query timeout | Increase QueryTimeoutSeconds in appsettings.json |
| Too many rows | Result is truncated; increase MaxRowLimit or add WHERE clause |

## 7. Next Steps

- [ ] Update connection string with real password
- [ ] Start the MCP server
- [ ] Run unit tests to verify setup
- [ ] Test with curl or Postman
- [ ] Integrate MCP client into your AI agents
- [ ] Monitor logs for MCP operations


## Summary
I've created a comprehensive testing and usage guide for your MCP implementation:
Test Files Created:
	1.	MCPServerTests.cs - 8 integration tests covering all MCP functionality
	2.	MCPClientServiceTests.cs - Unit tests with mocked HTTP responses
	3.	MCPConsoleExample.cs - Runnable console application demonstrating usage
	4.	MANUAL_TESTING_GUIDE.md - curl and Postman examples
	5.	QUICK_START.md - Setup and troubleshooting guide

## What You Can Test:
	✅ Tool discovery (tools.list) 
	✅ Schema retrieval (schema.describe) 
	✅ Query execution (sql.execute_readonly) 
	✅ Security validation (rejecting DML/DDL) 
	✅ Error handling 
	✅ Client-side caching 
	✅ Performance monitoring

## How to Get Started:
	1.	Update appsettings.json with the actual readonly user password
	2.	Run dotnet test to execute unit tests
	3.	Start the server with dotnet run
	4.	Test endpoints with curl or Postman (examples provided)

All responses follow the MCP JSON-RPC 2.0 standard and include proper logging!


