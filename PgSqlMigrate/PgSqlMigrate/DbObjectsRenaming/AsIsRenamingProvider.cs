using PgSqlMigrate.Models;

namespace PgSqlMigrate.DbObjectsRenaming
{
    /// <summary>
    /// As IS renaming provider. Keep original names
    /// </summary>
    public class AsIsRenamingProvider : IDbObjectRenamingProvider
    {
        public OnObjectRenamed? OnRenamed { get; set; }

        public ConstraintInfo ConvertConstraint(ConstraintInfo constraint)
        {
            return constraint;
        }

        public string NormalizeName(string name, out bool isNormalized)
        {
            isNormalized = false;
            return name;
        }

        public string? ConvertName(string schemaName, string? oldName, DbObjectType objectType)
        {
            return oldName;
        }

        public string GetColumnName(string oldName)
        {
            return oldName;
        }

        public string GetTableName(string schemaName, string oldName)
        {
            return oldName;
        }

        public IndexInfo ConvertIndex(IndexInfo index)
        {
            return index;
        }

        public IdentityColumnInfo ConvertAutoincrementColumn(IdentityColumnInfo identity)
        {
            return identity;
        }
    }
}
