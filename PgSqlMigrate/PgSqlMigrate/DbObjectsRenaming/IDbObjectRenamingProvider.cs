using PgSqlMigrate.Models;

namespace PgSqlMigrate.DbObjectsRenaming
{
    /// <summary>
    /// DB Objects renaming provider
    /// </summary>
    public interface IDbObjectRenamingProvider
    {
        /// <summary>
        /// Get new schema name
        /// </summary>
        /// <param name="oldName"></param>
        /// <returns></returns>
        string GetSchemaName(string oldName);

        /// <summary>
        /// Get new table name
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="oldName"></param>
        /// <returns></returns>
        string GetTableName(string schemaName, string oldName);

        /// <summary>
        /// Get new column name
        /// </summary>
        /// <param name="oldName"></param>
        /// <returns></returns>
        string GetColumnName(string oldName);

        /// <summary>
        /// Default conversion
        /// </summary>
        /// <param name="oldName">Old name</param>
        /// <returns></returns>
        string? ConvertName(string schemaName, string? oldName, DbObjectType objectType);

        /// <summary>
        /// Convert constraint by renaming all used objects
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        ConstraintInfo ConvertConstraint(ConstraintInfo constraint);

        /// <summary>
        /// Convert index by renaming all used objects
        /// </summary>
        /// <param name="index">Index definition</param>
        /// <returns></returns>
        IndexInfo ConvertIndex(IndexInfo index);

        /// <summary>
        /// Renaming event handler
        /// </summary>
        OnObjectRenamed? OnRenamed { set; }

        /// <summary>
        /// Convert autoincrement column definition
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        IdentityColumnInfo ConvertAutoincrementColumn(IdentityColumnInfo identity);
    }

    public delegate void OnObjectRenamed(string schemaName, DbObjectType objectType, string oldName, string newName, bool normalized);
}
