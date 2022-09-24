using PgSqlMigrate.Models;
using System.Data;
using System.Data.Common;

namespace PgSqlMigrate
{
    /// <summary>
    /// DDL manager
    /// </summary>
    public interface IDdlManager
    {
        /// <summary>
        /// Get unique keys
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <returns></returns>
        Task<List<ConstraintInfo>> RetrieveUniqueKeysAsync(string tableName);

        /// <summary>
        /// Get default constraints
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columnName">Column name</param>
        /// <returns></returns>
        Task<List<ConstraintInfo>> RetrieveDefaultConstraintsAsync(string tableName, string columnName);

        /// <summary>
        /// Get indexes
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<List<IndexInfo>> RetrieveIndexesAsync(string tableName);

        /// <summary>
        /// Get column type
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        Task<string?> RetrieveColumnTypeAsync(string table, string column);

        /// <summary>
        /// Get table names
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        Task<List<string>> GetTableNamesAsync(string? schemaName);


        /// <summary>
        /// Get schemas
        /// </summary>
        /// <returns></returns>
        Task<List<SchemaInfo>> GetSchemasAsync();

        /// <summary>
        /// Create schema
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task CreateSchemaAsync(string name);

        /// <summary>
        /// Get table columns
        /// </summary>
        /// <returns></returns>
        Task<List<TableColumn>> GetTableColumnsAsync(string schema, string table);

        /// <summary>
        /// Create table with specified list of columns
        /// </summary>
        /// <param name="table">Table name</param>
        /// <param name="columns">Columns</param>
        /// <returns></returns>
        Task CreateTableAsync(string schema, string table, List<TableColumn> columns);

        /// <summary>
        /// Get column data type definition, is used to create table column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        string GetColumnDataTypeDefinition(TableColumn column);

        /// <summary>
        /// Drop table if exists
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        Task DropTableIfExists(string schema, string table);

        DbCommand CreateQuery(string sql);
        Task ExecuteQueryAsync(string sql, Func<IDataReader, Task> action);
        Task ExecuteQueryAsync(string sql, Action<IDataReader> action);
        Task<object?> ExecuteScalarAsync(string sql);
        Task ExecuteNonQueryAsync(string sql, int? commandTimeout = null);
        string GetFullName(string schema, string objectName);
        DbTransaction BeginTransaction();
    }
}
