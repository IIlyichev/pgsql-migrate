using Microsoft.EntityFrameworkCore;
using PgSqlMigrate.Extensions;
using PgSqlMigrate.Models;
using PgSqlMigrate.TypeMaps;
using PgSqlMigrate.TypeMaps.Postgres;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace PgSqlMigrate.PostgreSql
{
    public class PostgreSqlDdlManager : DdlManagerBase, IDdlWriter
    {
        public PostgreSqlDdlManager(DbContext dbContext, bool useCiText) : base(dbContext, new PostgresTypeMap(useCiText))
        {
        }

        public async Task CreateTableAsync(string schema, string table, List<TableColumn> columns)
        {
            await DropTableIfExistsAsync(schema, table);

            var sb = new StringBuilder();
            

            sb.AppendLine(@$"create table {GetFullName(schema, table)} (");

            var lastColumn = columns.LastOrDefault();
            foreach (var column in columns) 
            {
                var nullability = column.IsNullable ? "null" : "not null";
                var dataType = GetColumnDataTypeDefinition(column);
                sb.Append(@$"    ""{column.Name}"" {dataType} {nullability}");

                sb.AppendLine(column != lastColumn
                    ? ","
                    : "");
            }
            
            sb.AppendLine($")");

            try
            {
                await ExecuteNonQueryAsync(sb.ToString());
            }
            catch (Exception e) 
            {
                throw;
            }            
        }

        public string GetColumnDataTypeDefinition(TableColumn column)
        {
            var size = column.DataType == DbType.Int32 || column.DataType == DbType.Int64
                ? null
                : column.Size;

            if (!column.IsUserDefinedType)
            {
                if (column.DataType == null)
                    throw new ArgumentException($"{nameof(column.DataType)} must not be null for standard data types");
                return _typeMap.GetTypeMap(column.DataType.Value, size, column.Precision);
            }
            else
            {
                // handle custom data types
                return _typeMap.GetCustomTypeMap(column.DataTypeDefinition, size, column.Precision);
            }
        }

            public async Task DropTableIfExistsAsync(string schema, string table)
        {
            await ExecuteNonQueryAsync($"DROP TABLE IF EXISTS {GetFullName(schema, table)}");
        }

        public async Task DropViewIfExistsAsync(string schema, string viewName)
        {
            await ExecuteNonQueryAsync($"DROP VIEW IF EXISTS {GetFullName(schema, viewName)}");
        }

        public async Task CreateSchemaAsync(string name)
        {
            await ExecuteNonQueryAsync(@$"create schema if not exists ""{name}""");
        }

        /// inheritedDoc
        public async Task CreatePrimaryKeyAsync(ConstraintInfo constraint) 
        {
            var columns = GetColumnsDelimitedList(constraint.Fields);
            var sql = $@"ALTER TABLE {constraint.Schema.Quote()}.{constraint.TableName.Quote()} ADD PRIMARY KEY ({columns})";
            await ExecuteNonQueryAsync(sql);
        }

        public async Task CreateUniqueKeyAsync(ConstraintInfo constraint)
        {
            var columns = GetColumnsDelimitedList(constraint.Fields);
            var sql = $@"ALTER TABLE {constraint.Schema.Quote()}.{constraint.TableName.Quote()} ADD  CONSTRAINT {constraint.ConstraintName.Quote()} UNIQUE ({columns})";
            await ExecuteNonQueryAsync(sql);
        }

        public async Task CreateForeignKeyAsync(ConstraintInfo constraint)
        {
            var slaveColumns = GetColumnsDelimitedList(constraint.Fields);
            var primaryColumns = GetColumnsDelimitedList(constraint.PrimaryTableFields);

            var sql = $@"ALTER TABLE 
	{GetFullName(constraint.Schema, constraint.TableName)}
ADD CONSTRAINT 
	{constraint.ConstraintName.Quote()}
FOREIGN KEY 
	({slaveColumns}) 
REFERENCES 
	{GetFullName(constraint.PrimaryTableSchemaName, constraint.PrimaryTableName)} ({primaryColumns})
ON UPDATE {constraint.UpdateRule} 
ON DELETE {constraint.DeleteRule}";
            
            await ExecuteNonQueryAsync(sql);
        }

        private string GetColumnsDelimitedList(List<string> columns) 
        {
            return string.Join(',', columns.Select(f => f.Quote()));
        }

        public async Task CreateIndexAsync(IndexInfo index)
        {
            var columns = GetColumnsDelimitedList(index.Columns);
            var includedColumns = GetColumnsDelimitedList(index.IncludedColumns);

            var sql = $@"create index {index.Name.Quote()} on {GetFullName(index.Schema, index.Table)} ({columns})";

            if (index.IncludedColumns.Any())
                sql += $" include ({includedColumns})";

            await ExecuteNonQueryAsync(sql, 60*10);
        }

        public async Task MakeColumnAutoincrementAsync(IdentityColumnInfo identity)
        {
            var startWith = (identity.LastValue ?? 0) + 1;
            var sql = 
$@"ALTER TABLE {GetFullName(identity.Schema, identity.Table)} ALTER COLUMN {identity.ColumnName.Quote()} ADD GENERATED BY DEFAULT AS IDENTITY (START WITH {startWith} INCREMENT BY {identity.SeedIncrement})";

            await ExecuteNonQueryAsync(sql, 60 * 10);
        }

        private string ConvertDefaultDefinition(string definition, DbType? valueType) 
        {
            if (string.IsNullOrWhiteSpace(definition))
                return definition;
            
            if (definition.Equals("(newsequentialid())", StringComparison.InvariantCultureIgnoreCase))
                return "uuid_generate_v4()";

            if (definition.Equals("(getdate())", StringComparison.InvariantCultureIgnoreCase))
                return "CURRENT_TIMESTAMP";

            var numericRegex = new Regex(@"\(\((?<value>[\d]+)\)\)");
            var numericMatch = numericRegex.Match(definition);
            if (numericMatch.Success)
            {
                var value = numericMatch.Groups["value"].Value;
                if (valueType == DbType.Boolean)
                {
                    return int.TryParse(value, out var intValue) && intValue > 0 ? "true" : "false";
                }
                else
                    return value;
            }

            var dateRegex = new Regex(@"\(\((?<year>\d{4})\)-\((?<month>\d{1,2})\)\)-\((?<day>\d{1,2})\)\)");
            var dateMatch = dateRegex.Match(definition);
            if (dateMatch.Success)
            {
                return $"to_timestamp('{dateMatch.Groups["year"]}-{dateMatch.Groups["month"]}-{dateMatch.Groups["day"]}', 'YYYY-MM-DD')";
            }

            if (valueType == DbType.String)
            {
                var nvarcharRegex = new Regex(@"\(N'(?<value>[^]]*)'\)");
                var nvarcharMatch = nvarcharRegex.Match(definition);
                if (nvarcharMatch.Success)
                    return "'" + nvarcharMatch.Groups["value"].Value + "'";
            }

            return null;
        }

        private async Task<string> GetColumnDataTypeAsync(string schema, string table, string column) 
        {
            var sql = 
$@"select
	data_type 
from
	information_schema.columns 
where	
	table_schema = '{schema}'
	and table_name = '{table}'
	and column_name = '{column}'";
            return (string)await ExecuteScalarAsync(sql);
        }

        public async Task CreateDefaultConstraintAsync(ConstraintInfo defaultConstraint)
        {
            if (defaultConstraint.Fields.Count() != 1)
                throw new NotSupportedException("Default constraints must contain single column");

            var column = defaultConstraint.Fields.FirstOrDefault();

            //var dataType = await GetColumnDataTypeAsync(defaultConstraint.Schema, defaultConstraint.TableName, column);

            // convert definitions
            var defaultDefinition = ConvertDefaultDefinition(defaultConstraint.Definition, defaultConstraint.ValueType);
            if (string.IsNullOrWhiteSpace(defaultDefinition))
                throw new NotSupportedException($"Failed to convert default definition '{defaultConstraint.Definition}'");

            var sql = $@"alter table {GetFullName(defaultConstraint.Schema, defaultConstraint.TableName)} alter column ""{column}"" set default {defaultDefinition}";
            try
            {
                await ExecuteNonQueryAsync(sql, 60 * 10);
            }
            catch (Exception e) 
            {
                throw;
            }            
        }

        private Dictionary<string, DbType> _reservedTypeMap;
        public Dictionary<string, DbType> DbTypesMap
        {
            get
            {
                return _reservedTypeMap ?? (_reservedTypeMap = TypeMapHelper.GetReversedTypesMap(_typeMap, (typeName, dbTypes) => dbTypes.FirstOrDefault()));
            }
        }
    }
}
