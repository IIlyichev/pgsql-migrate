using PgSqlMigrate.Extensions;
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
            var tableName = GetTableName(constraint.Schema, constraint.TableName);

            if (string.IsNullOrWhiteSpace(constraint.ConstraintName))
                throw new NotSupportedException("ConstraintName must be specified");

            var result = new ConstraintInfo(constraint.Schema, tableName, ConvertName(constraint.Schema, constraint.ConstraintName, constraint.Type.ToDbObjectType()), constraint.Type);
            result.PrimaryTableName = ConvertName(constraint.Schema, constraint.PrimaryTableName, DbObjectType.Table);

            if (constraint.PrimaryTableFields != null)
                result.PrimaryTableFields = constraint.PrimaryTableFields.Select(f => GetColumnName(f)).ToList();
            if (constraint.Fields != null)
                result.Fields = constraint.Fields.Select(f => GetColumnName(f)).ToList();

            result.Schema = GetSchemaName(constraint.Schema);
            result.PrimaryTableSchemaName = GetSchemaName(constraint.PrimaryTableSchemaName);
            result.UpdateRule = constraint.UpdateRule;
            result.DeleteRule = constraint.DeleteRule;

            result.Definition = constraint.Definition;
            result.ValueType = constraint.ValueType;

            return result;

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
            var newSchema = GetSchemaName(index.Schema);
            var result = new IndexInfo(newSchema,
                ConvertName(index.Schema, index.Table, DbObjectType.Table),
                ConvertName(index.Schema, index.Name, DbObjectType.Index));

            result.Columns = index.Columns.Select(f => ConvertName(index.Schema, f, DbObjectType.Column)).ToList();
            result.IncludedColumns = index.IncludedColumns.Select(f => ConvertName(index.Schema, f, DbObjectType.Column)).ToList();
            return result;
        }

        public IdentityColumnInfo ConvertAutoincrementColumn(IdentityColumnInfo identity)
        {
            var newSchema = GetSchemaName(identity.Schema);
            return new IdentityColumnInfo(
                newSchema,
                GetTableName(identity.Schema, identity.Table),
                GetColumnName(identity.ColumnName),
                identity.SeedValue,
                identity.SeedIncrement,
                identity.LastValue
            );
        }

        public string GetSchemaName(string oldName)
        {
            return !string.IsNullOrWhiteSpace(oldName) && oldName.Equals("dbo", StringComparison.InvariantCultureIgnoreCase)
                ? "public"
                : oldName;
        }
    }
}
