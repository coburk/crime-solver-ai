namespace MCP.MSSQL.Server.Models
{
 /// <summary>
    /// Represents a database column in the schema description.
    /// Used by schema.describe MCP tool to provide column metadata.
    /// </summary>
    public class ColumnInfo
    {
     /// <summary>Gets or sets the column name.</summary>
     public string? ColumnName { get; set; }

  /// <summary>Gets or sets the data type of the column (e.g., "VARCHAR", "INT").</summary>
        public string? DataType { get; set; }

       /// <summary>Gets or sets the maximum character length for string types, or null for other types.</summary>
   public int? MaxLength { get; set; }

 /// <summary>Gets or sets whether the column allows null values.</summary>
        public bool IsNullable { get; set; }

     /// <summary>Gets or sets whether the column is part of the primary key.</summary>
    public bool IsPrimaryKey { get; set; }

   /// <summary>Gets or sets whether the column is an identity column (auto-increment).</summary>
 public bool IsIdentity { get; set; }
    }

    /// <summary>
   /// Represents a foreign key relationship between tables.
   /// Used by schema.describe MCP tool to provide relationship metadata.
/// </summary>
    public class ForeignKeyInfo
    {
        /// <summary>Gets or sets the name of the foreign key constraint.</summary>
      public string? ConstraintName { get; set; }

    /// <summary>Gets or sets the table that contains the foreign key column.</summary>
        public string? FromTable { get; set; }

  /// <summary>Gets or sets the column in the FROM table that references another table.</summary>
  public string? FromColumn { get; set; }

   /// <summary>Gets or sets the table being referenced.</summary>
  public string? ToTable { get; set; }

        /// <summary>Gets or sets the primary key column in the TO table being referenced.</summary>
     public string? ToColumn { get; set; }
    }

    /// <summary>
    /// Represents a table in the database schema.
    /// Used by schema.describe MCP tool to provide complete table metadata.
    /// </summary>
    public class TableSchema
    {
   /// <summary>Gets or sets the table name.</summary>
   public string? TableName { get; set; }

    /// <summary>Gets or sets the list of columns in the table.</summary>
      public List<ColumnInfo> Columns { get; set; } = new();

      /// <summary>Gets or sets the list of foreign keys in the table.</summary>
        public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();

     /// <summary>Gets or sets the number of rows in the table.</summary>
        public long RowCount { get; set; }
    }

    /// <summary>
    /// Response model for the schema.describe MCP tool.
    /// Contains complete database schema information including tables, columns, and relationships.
 /// Implements MCP standard response format for schema introspection.
    /// </summary>
    public class SchemaDescribeResponse
    {
        /// <summary>Gets or sets the database name.</summary>
   public string? DatabaseName { get; set; }

   /// <summary>Gets or sets the timestamp when the schema was retrieved.</summary>
        public DateTime RetrievedAt { get; set; }

  /// <summary>Gets or sets the list of all tables in the database with their metadata.</summary>
        public List<TableSchema> Tables { get; set; } = new();

     /// <summary>Gets or sets a summary message about the schema retrieval.</summary>
    public string? Summary { get; set; }
    }
}
