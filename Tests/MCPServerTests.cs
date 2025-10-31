using Xunit;
using MCP.MSSQL.Server;
using MCP.MSSQL.Server.Models;
using Microsoft.Extensions.Logging;

namespace CrimeSolverAI.Tests
{
    /// <summary>
    /// Integration tests for the MSSQL MCP Server.
    /// Tests tool discovery, schema retrieval, and query execution functionality.
    /// Requires a valid database connection configured in appsettings.json.
    /// </summary>
    public class MCPServerTests
    {
        private readonly MSSQLMCPServer _server;
        private readonly ILogger<MSSQLMCPServer> _logger;

        public MCPServerTests()
        {
            // Create a mock logger for testing
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<MSSQLMCPServer>();

            // Initialize server with test connection string
            // Try with explicit network protocol and connection timeout
            var connectionString = "Data Source=Burk7;Initial Catalog=SequelCityCrimesDB;User Id=UnitTest;Password=IL0veUnitTests!;Encrypt=true;TrustServerCertificate=false;Connection Timeout=10;";
            _server = new MSSQLMCPServer(connectionString, 30, 1000, _logger);
        }

        /// <summary>
        /// Tests that tools.list returns available tools with correct metadata.
        /// </summary>
        [Fact]
        public async Task ProcessRequest_ToolsList_ReturnsAvailableTools()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-1",
                Method = "tools.list",
                Params = new Dictionary<string, object>()
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal("2.0", response.Jsonrpc);
            Assert.Equal("test-1", response.Id);
            Assert.Null(response.Error);
            Assert.NotNull(response.Result);

            var toolsList = (response.Result as ToolsListResponse)
                ?? System.Text.Json.JsonSerializer.Deserialize<ToolsListResponse>(
                    System.Text.Json.JsonSerializer.Serialize(response.Result),
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new ToolsListResponse();

            Assert.NotEmpty(toolsList.Tools);
            Assert.Contains(toolsList.Tools, t => t.Name == "schema.describe");
            Assert.Contains(toolsList.Tools, t => t.Name == "sql.execute_readonly");
        }

        /// <summary>
        /// Tests that schema.describe returns complete database schema.
        /// SKIP: Requires live database connection to server "Burk7"
        /// </summary>
        [Fact(Skip = "Integration test requires live database connection to Burk7")]
        public async Task ProcessRequest_SchemaDescribe_ReturnsCompleteSchema()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-2",
                Method = "schema.describe",
                Params = new Dictionary<string, object>()
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Null(response.Error);

            var schema = System.Text.Json.JsonSerializer.Deserialize<SchemaDescribeResponse>(
                System.Text.Json.JsonSerializer.Serialize(response.Result),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(schema);
            Assert.Equal("SequelCityCrimesDB", schema?.DatabaseName);
            Assert.NotEmpty(schema?.Tables);
            
            // Verify each table has schema information
            if (schema?.Tables != null)
            {
                foreach (var table in schema.Tables)
                {
                    Assert.NotEmpty(table.TableName ?? "");
                    Assert.NotEmpty(table.Columns);
                    Assert.True(table.RowCount >= 0);
                }
            }
        }

        /// <summary>
        /// Tests that sql.execute_readonly executes SELECT queries successfully.
        /// SKIP: Requires live database connection to server "Burk7"
        /// </summary>
        [Fact(Skip = "Integration test requires live database connection to Burk7")]
        public async Task ProcessRequest_SQLExecuteReadOnly_ExecutesSelectQuery()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-3",
                Method = "sql.execute_readonly",
                Params = new Dictionary<string, object>
                {
                    { "query", "SELECT TOP 1 * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'" }
                }
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Null(response.Error);

            var executeResponse = System.Text.Json.JsonSerializer.Deserialize<SQLExecuteResponse>(
                System.Text.Json.JsonSerializer.Serialize(response.Result),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(executeResponse);
            Assert.True(executeResponse?.Success);
            Assert.NotEmpty(executeResponse?.Columns ?? new List<string>());
            Assert.True((executeResponse?.RowCount ?? 0) >= 0);
        }

        /// <summary>
        /// Tests that sql.execute_readonly rejects INSERT statements.
        /// </summary>
        [Fact]
        public async Task ProcessRequest_SQLExecuteReadOnly_RejectsInsertStatement()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-4",
                Method = "sql.execute_readonly",
                Params = new Dictionary<string, object>
                {
                    { "query", "INSERT INTO SomeTable VALUES (1)" }
                }
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Contains("Only SELECT queries are allowed", response.Error.Message ?? "");
        }

        /// <summary>
        /// Tests that sql.execute_readonly rejects DELETE statements.
        /// </summary>
        [Fact]
        public async Task ProcessRequest_SQLExecuteReadOnly_RejectsDeleteStatement()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-5",
                Method = "sql.execute_readonly",
                Params = new Dictionary<string, object>
                {
                    { "query", "DELETE FROM SomeTable WHERE Id = 1" }
                }
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Contains("Only SELECT queries are allowed", response.Error.Message ?? "");
        }

        /// <summary>
        /// Tests that sql.execute_readonly rejects UPDATE statements.
        /// </summary>
        [Fact]
        public async Task ProcessRequest_SQLExecuteReadOnly_RejectsUpdateStatement()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-6",
                Method = "sql.execute_readonly",
                Params = new Dictionary<string, object>
                {
                    { "query", "UPDATE SomeTable SET Name = 'Test'" }
                }
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
        }

        /// <summary>
        /// Tests that invalid method names return proper error response.
        /// </summary>
        [Fact]
        public async Task ProcessRequest_InvalidMethod_ReturnsMethodNotFoundError()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-7",
                Method = "invalid.method",
                Params = new Dictionary<string, object>()
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Equal(-32601, response.Error.Code);
            Assert.Contains("not found", response.Error.Message ?? "");
        }

        /// <summary>
        /// Tests that missing query parameter returns proper error.
        /// </summary>
        [Fact]
        public async Task ProcessRequest_SQLExecuteReadOnly_MissingQueryParameter_ReturnsError()
        {
            // Arrange
            var request = new MCPRequest
            {
                Id = "test-8",
                Method = "sql.execute_readonly",
                Params = new Dictionary<string, object>()
            };

            // Act
            var response = await _server.ProcessRequestAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Equal(-32602, response.Error.Code);
            Assert.Contains("query", response.Error.Message?.ToLower() ?? "");
        }
    }
}