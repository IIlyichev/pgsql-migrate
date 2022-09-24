using Microsoft.EntityFrameworkCore;
using PgSqlMigrate.Models;
using PgSqlMigrate.TypeMaps;
using PgSqlMigrate.TypeMaps.Postgres;
using System.Data;
using System.Text;

namespace PgSqlMigrate.PostgreSql
{
    public class PostgreSqlDdlManager : DdlManagerBase, IDdlManager
    {
        public PostgreSqlDdlManager(DbContext dbContext) : base(dbContext, new PostgresTypeMap())
        {
        }

        public async Task CreateTableAsync(string schema, string table, List<TableColumn> columns)
        {
            await DropTableIfExists(schema, table);

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
            var size = column.DataType == DbType.Int32
                ? null
                : column.Size;
            return _typeMap.GetTypeMap(column.DataType, size, column.Precision);
        }

        public Task<List<SchemaInfo>> GetSchemasAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetTableNamesAsync(string? schemaName)
        {
            throw new NotImplementedException();
        }

        public Task<string?> RetrieveColumnTypeAsync(string table, string column)
        {
            throw new NotImplementedException();
        }

        public Task<List<ConstraintInfo>> RetrieveDefaultConstraintsAsync(string tableName, string columnName)
        {
            throw new NotImplementedException();
        }

        public Task<List<IndexInfo>> RetrieveIndexesAsync(string tableName)
        {
            throw new NotImplementedException();
        }

        public Task<List<ConstraintInfo>> RetrieveUniqueKeysAsync(string tableName)
        {
            throw new NotImplementedException();
        }

        public async Task DropTableIfExists(string schema, string table)
        {
            await ExecuteNonQueryAsync($"DROP TABLE IF EXISTS {GetFullName(schema, table)}");
        }

        public Task<List<TableColumn>> GetTableColumnsAsync(string schema, string table)
        {
            throw new NotImplementedException();
        }

        public async Task CreateSchemaAsync(string name)
        {
            await ExecuteNonQueryAsync(@$"create schema if not exists ""{name}""");
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
