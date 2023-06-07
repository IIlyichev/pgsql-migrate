using PgSqlMigrate.Models;

namespace PgSqlMigrate
{
    /// <summary>
    /// DDL reader
    /// </summary>
    public interface IDdlReader: IDdlManagerBase
    {
        /// <summary>
        /// Get unique keys
        /// </summary>
        /// <param name="schema">Schema name</param>
        /// <param name="tableName">Table name</param>
        /// <returns></returns>
        Task<List<ConstraintInfo>> RetrieveUniqueKeysAsync(string schema, string tableName);

        /// <summary>
        /// Get foreign keys
        /// </summary>
        /// <param name="schema">Schema name</param>
        /// <param name="tableName">Table name</param>
        /// <returns></returns>
        Task<List<ConstraintInfo>> RetrieveForeignKeysAsync(string schema, string tableName);

        /// <summary>
        /// Get primary key definition
        /// </summary>
        /// <param name="schema">Schema name</param>
        /// <param name="tableName">Table Name</param>
        /// <returns></returns>
        Task<ConstraintInfo?> RetrievePrimaryKeyDefinition(string schema, string tableName);

        /// <summary>
        /// Get default constraints
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columnName">Column name</param>
        /// <returns></returns>
        Task<List<ConstraintInfo>> RetrieveDefaultConstraintsAsync(string schema, string tableName, string columnName);

        /// <summary>
        /// Get indexes
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<List<IndexInfo>> RetrieveIndexesAsync(string schema, string tableName);

        /// <summary>
        /// Get indexes
        /// </summary>
        /// <param name="schema">Schema name</param>
        /// <returns></returns>
        Task<List<IndexInfo>> RetrieveIndexes(string schema);

        /// <summary>
        /// Get auto-created statistics
        /// </summary>
        /// <param name="schema">Schema name</param>
        /// <returns></returns>
        Task<List<StatisticsInfo>> RetrieveAutoCreatedStatistics(string schema);

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
        Task<List<string>> GetTableNamesAsync(string schemaName);


        /// <summary>
        /// Get schemas
        /// </summary>
        /// <returns></returns>
        Task<List<SchemaInfo>> GetSchemasAsync();

        /// <summary>
        /// Returns true is the specified schema is empty
        /// </summary>
        /// <returns></returns>
        Task<bool> IsSchemaEmpty(string schemaName);

        /// <summary>
        /// Get table columns
        /// </summary>
        /// <returns></returns>
        Task<List<TableColumn>> GetTableColumnsAsync(string schema, string table);

        /// <summary>
        /// Get view definitions
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        Task<List<SqlModuleDefinition>> GetViewDefinitionsAsync(string schemaName);

        /// <summary>
        /// Get scalar function definitions
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        Task<List<SqlModuleDefinition>> GetScalarFunctionDefinitionsAsync(string schemaName);

        /// <summary>
        /// Get stored procedure definitions
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        Task<List<SqlModuleDefinition>> GetStoredProcedureDefinitionsAsync(string schemaName);

        /// <summary>
        /// Get table function definitions
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        Task<List<SqlModuleDefinition>> GetTableFunctionDefinitionsAsync(string schemaName);

        /// <summary>
        /// Get inline table function definitions
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        Task<List<SqlModuleDefinition>> GetInlineTableFunctionDefinitionsAsync(string schemaName);

        /// <summary>
        /// Get table identity columns
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        Task<List<IdentityColumnInfo>> GetTableAutoincrementColumnsAsync(string schema, string table);
    }
}
