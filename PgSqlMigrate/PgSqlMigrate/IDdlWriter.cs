using PgSqlMigrate.Models;

namespace PgSqlMigrate
{
    /// <summary>
    /// DDL writer
    /// </summary>
    public interface IDdlWriter: IDdlManagerBase
    {
        /// <summary>
        /// Create schema
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task CreateSchemaAsync(string name);

        /// <summary>
        /// Create table with specified list of columns
        /// </summary>
        /// <param name="table">Table name</param>
        /// <param name="columns">Columns</param>
        /// <returns></returns>
        Task CreateTableAsync(string schema, string table, List<TableColumn> columns);

        /// <summary>
        /// Drop table if exists
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        Task DropTableIfExistsAsync(string schema, string table);

        /// <summary>
        /// Drop view if exists
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        Task DropViewIfExistsAsync(string schema, string viewName);

        /// <summary>
        /// Create primary key
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        Task CreatePrimaryKeyAsync(ConstraintInfo constraint);

        /// <summary>
        /// Create unique key
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        Task CreateUniqueKeyAsync(ConstraintInfo constraint);

        /// <summary>
        /// Create index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Task CreateIndexAsync(IndexInfo index);

        /// <summary>
        /// Create foreign key
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        Task CreateForeignKeyAsync(ConstraintInfo constraint);
        
        /// <summary>
        /// Make column autoincrement
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        Task MakeColumnAutoincrementAsync(IdentityColumnInfo identity);

        /// <summary>
        /// Create default constraint
        /// </summary>
        /// <param name="defaultConstraint"></param>
        /// <returns></returns>
        Task CreateDefaultConstraintAsync(ConstraintInfo defaultConstraint);
    }
}
