# MCP Manual Testing Guide

## Prerequisites
1. Ensure the application is running on `http://localhost:5000`
2. Update `appsettings.json` with valid readonly user password
3. Verify database connection: `Burk7\SequelCityCrimesDB`

## Testing Tools

### Option 1: Using curl (Command Line)

#### 1. Test tools.list
curl -X POST http://localhost:5000/mcp/invoke 
-H "Content-Type: application/json" 
-d '{ "jsonrpc": "2.0", "id": "1", "method": "tools.list", "params": {} }'

**Expected Response:**
{ "jsonrpc": "2.0", "id": "1", "result": { "tools": [ { "name": "schema.describe",
"description": "Returns database schema information...", "inputSchema": [] }, 
{ "name": "sql.execute_readonly", "description": "Executes read-only SELECT queries...", 
"inputSchema": [ { "name": "query", "type": "string", "description": "The SELECT query to execute", 
"required": true } ] } ] } }

#### 2. Test schema.describe
curl -X POST http://localhost:5000/mcp/invoke 
-H "Content-Type: application/json" 
-d '{ "jsonrpc": "2.0", "id": "2", "method": "schema.describe", "params": {} }'

**Expected Response:**
{ "jsonrpc": "2.0", "id": "2", "result": { "databaseName": "SequelCityCrimesDB", "retrievedAt": "2024-10-28T10:30:00Z", "tables": [ { "tableName": "Cases", "columns": [ { "columnName": "CaseId", "dataType": "INT", "maxLength": null, "isNullable": false, "isPrimaryKey": true, "isIdentity": true } ], "foreignKeys": [], "rowCount": 42 } ], "summary": "Retrieved schema for X tables with Y columns..." } }


#### 3. Test sql.execute_readonly with SELECT
curl -X POST http://localhost:5000/mcp/invoke 
-H "Content-Type: application/json" 
-d '{ "jsonrpc": "2.0", "id": "3", "method": "sql.execute_readonly", "params": { "query": "SELECT TOP 5 * FROM Cases" } }'

**Expected Response:**
{ "jsonrpc": "2.0", "id": "3", "result": { "success": true, "query": "SELECT TOP 5 * FROM Cases", "rowCount": 5, "maxRowLimit": 1000, "isTruncated": false, "columns": ["CaseId", "CaseName", "Status"], "rows": [ { "values": { "CaseId": 1, "CaseName": "Murder at Downtown", "Status": "Open" } } ], "executionTimeMs": 125, "summary": "Query returned 5 row(s) out of 5 total row(s)." } }


#### 4. Test security: Reject INSERT
curl -X POST http://localhost:5000/mcp/invoke 
-H "Content-Type: application/json" 
-d '{ "jsonrpc": "2.0", "id": "4", "method": "sql.execute_readonly", "params": { "query": "INSERT INTO Cases VALUES (999, "Fake Case")" } }

**Expected Response (Error):**
{ "jsonrpc": "2.0", "id": "4", "error": { "code": -32602, "message": "Only SELECT queries are allowed. DML/DDL statements are not permitted." } }


#### 5. Test security: Reject DELETE
curl -X POST http://localhost:5000/mcp/invoke 
-H "Content-Type: application/json" 
-d '{ "jsonrpc": "2.0", "id": "5", "method": "sql.execute_readonly", "params": { "query": "DELETE FROM Cases WHERE CaseId = 1" } }'


**Expected Response (Error):**
{ "jsonrpc": "2.0", "id": "5", "error": { "code": -32602, "message": "Only SELECT queries are allowed. DML/DDL statements are not permitted." } }




### Option 2: Using Postman

1. **Create a new POST request:**
   - URL: `http://localhost:5000/mcp/invoke`
   - Header: `Content-Type: application/json`

2. **Body (tools.list):**
   ```json
   { "jsonrpc": "2.0", "id": "1", "method": "tools.list", "params": {} }
   ```

3. **Body (schema.describe):**
   ```json
   { "jsonrpc": "2.0", "id": "2", "method": "schema.describe", "params": {} }
   ```


4. **Body (sql.execute_readonly):**
   ```json
   { "jsonrpc": "2.0", "id": "3", "method": "sql.execute_readonly", "params": { "query": "SELECT COUNT(*) as CaseCount FROM Cases" } }
   ```


## Testing Checklist

- [ ] `tools.list` returns both available tools
- [ ] `schema.describe` returns all tables with columns
- [ ] `sql.execute_readonly` executes SELECT queries
- [ ] `sql.execute_readonly` returns correct row count
- [ ] `sql.execute_readonly` includes execution time
- [ ] INSERT queries are rejected
- [ ] UPDATE queries are rejected
- [ ] DELETE queries are rejected
- [ ] CREATE queries are rejected
- [ ] DROP queries are rejected
- [ ] Missing query parameter returns error
- [ ] Invalid method returns "not found" error
- [ ] Response follows JSON-RPC 2.0 format
- [ ] All responses have `jsonrpc: "2.0"` and matching `id`

## Troubleshooting

### Connection Errors
- Verify server is running: `http://localhost:5000`
- Check `appsettings.json` connection string
- Verify readonly user exists in SQL Server
- Test SQL connection manually

### Query Timeouts
- Increase `QueryTimeoutSeconds` in `appsettings.json`
- Optimize the query
- Check database performance

### Row Limit Warnings
- Check `IsTruncated` flag in response
- Increase `MaxRowLimit` if needed (default: 1000)
- Consider filtering results with WHERE clause