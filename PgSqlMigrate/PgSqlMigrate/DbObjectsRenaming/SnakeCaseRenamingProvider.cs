using PgSqlMigrate.Extensions;
using PgSqlMigrate.Models;

namespace PgSqlMigrate.DbObjectsRenaming
{
    /// <summary>
    /// SnakeCase renaming provider. Renames all objects to snake_case
    /// </summary>
    public class SnakeCaseRenamingProvider : IDbObjectRenamingProvider
    {
        public OnObjectRenamed? OnRenamed { get; set; }

        private const int PgSqlNameMaxLength = 63;
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

            result.PrimaryTableSchemaName = constraint.PrimaryTableSchemaName;
            result.UpdateRule = constraint.UpdateRule;
            result.DeleteRule = constraint.DeleteRule;

            result.Definition = constraint.Definition;
            result.ValueType = constraint.ValueType;

            return result;
        }

        public string NormalizeName(string name, out bool isNormalized)
        {

            var normalized = name.RemoveDoubleUndescores();
            if (name.Length > PgSqlNameMaxLength) 
            {
                // todo: add generic normalization

                if (normalized.Length > PgSqlNameMaxLength)
                    throw new Exception($"Failed to normalize name '{name}' (length = {name.Length}), result of normalization: '{normalized}' (length = {normalized.Length}). Max length is {PgSqlNameMaxLength}");
            }
            isNormalized = normalized != name;
            return normalized;
        }

        public string? ConvertName(string schemaName, string? oldName, DbObjectType objectType)
        {
            if (oldName == null)
                return null;

            var newName = oldName.ToSnakeCase();
            newName = NormalizeName(newName, out var isNormalized);

            OnRenamed?.Invoke(schemaName, objectType, oldName, newName, isNormalized);
            return newName;
        }

        public string GetColumnName(string oldName)
        {
            return oldName.RemoveDoubleUndescores().ToSnakeCase();
        }

        public string GetTableName(string schemaName, string oldName)
        {
            var newName = oldName.ToSnakeCase();
            newName = NormalizeName(newName, out var isNormalized);

            OnRenamed?.Invoke(schemaName, DbObjectType.Table, oldName, newName, isNormalized);
            return newName;
        }

        public IndexInfo ConvertIndex(IndexInfo index)
        {
            var result = new IndexInfo(index.Schema, 
                ConvertName(index.Schema, index.Table, DbObjectType.Table), 
                ConvertName(index.Schema, index.Name, DbObjectType.Index));

            result.Columns = index.Columns.Select(f => ConvertName(index.Schema, f, DbObjectType.Column)).ToList();
            result.IncludedColumns = index.IncludedColumns.Select(f => ConvertName(index.Schema, f, DbObjectType.Column)).ToList();
            return result;
        }

        public IdentityColumnInfo ConvertAutoincrementColumn(IdentityColumnInfo identity)
        {
            return new IdentityColumnInfo(
                identity.Schema,
                GetTableName(identity.Schema, identity.Table),
                GetColumnName(identity.ColumnName),
                identity.SeedValue,
                identity.SeedIncrement,
                identity.LastValue
            );
        }
    }
}
