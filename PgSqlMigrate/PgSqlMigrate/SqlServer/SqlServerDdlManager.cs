using Microsoft.EntityFrameworkCore;
using PgSqlMigrate.Extensions;
using PgSqlMigrate.Models;
using PgSqlMigrate.TypeMaps;
using PgSqlMigrate.TypeMaps.SqlServer;
using System.Data;

namespace PgSqlMigrate.SqlServer
{
    /// <summary>
    /// Sql server DDL manager
    /// </summary>
    public class SqlServerDdlManager : DdlManagerBase, IDdlManager
    {
        public SqlServerDdlManager(DbContext dbContext) : base(dbContext, new SqlServer2008TypeMap())
        {
        }

        public async Task<List<string>> GetTableNamesAsync(string? schemaName)
        {
            var tables = new List<string>();


            var sql = string.IsNullOrWhiteSpace(schemaName)
                ? "SELECT sobjects.name FROM sysobjects sobjects WHERE sobjects.xtype = 'U' order by name"
                : string.Format(
                    "SELECT sobjects.name FROM sysobjects sobjects WHERE sobjects.xtype = 'U' and USER_NAME(uid) = '{0}' order by name",
                    schemaName);

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            });
            
            return tables;
        }

        public async Task<string?> RetrieveColumnTypeAsync(string table, string column)
        {
            var sql = $@"select data_type from information_schema.columns where table_name = '{table}' and column_name = '{column}'";

            var typeName = await ExecuteScalarAsync(sql);
            return (string?)typeName;
        }

        /// inhgeritDoc
        public async Task<List<ConstraintInfo>> RetrieveDefaultConstraintsAsync(string tableName, string columnName)
        {
            var defaults = new List<ConstraintInfo>();

            var sql = string.Format(@"
select d.name, d.definition
from sys.tables t
    join
    sys.default_constraints d
        on d.parent_object_id = t.object_id
    join
    sys.columns c
        on c.object_id = t.object_id
        and c.column_id = d.parent_column_id
where t.name = '{0}'
and c.name = '{1}'", tableName, columnName);

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var defName = reader.GetString(0);

                    var uq = defaults.FirstOrDefault(k => k.ConstraintName == defName);
                    if (uq == null)
                    {
                        uq = new ConstraintInfo(tableName, defName, ConstraintType.Default);
                        uq.Definition = reader.GetString(1);
                        defaults.Add(uq);
                    }

                    uq.Fields.Add(reader.GetString(1));
                }
            });

            return defaults;
        }

        /// inhgeritDoc
        public async Task<List<IndexInfo>> RetrieveIndexesAsync(string tableName)
        {
            var sql =
    $@"select 
	t.name tablename,
	i.name indexname,
	c.name columnname
from 
	sys.tables t
	inner join sys.indexes i on i.object_id = t.object_id
	inner join sys.index_columns ic on ic.object_id = t.object_id and ic.index_id = i.index_id
	inner join sys.columns c on t.object_id = c.object_id and c.column_id = ic.column_id
where t.name = '{tableName}'          
";

            var indexes = new List<IndexInfo>();
            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var idxName = reader.GetString(1);

                    var idx = indexes.FirstOrDefault(k => k.Name == idxName);
                    if (idx == null)
                    {
                        idx = new IndexInfo(tableName, idxName);
                        indexes.Add(idx);
                    }

                    idx.Fields.Add(reader.GetString(2));
                }
            });
            return indexes;
        }

        /// inhgeritDoc
        public async Task<List<ConstraintInfo>> RetrieveUniqueKeysAsync(string tableName)
        {
            var uqs = new List<ConstraintInfo>();

            var sql = string.Format(@"
select 
	tc.CONSTRAINT_NAME,
	cu.COLUMN_NAME
from 
	information_schema.TABLE_CONSTRAINTS tc left join information_schema.CONSTRAINT_COLUMN_USAGE cu on cu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
where 
	tc.CONSTRAINT_TYPE = 'UNIQUE'
	and tc.table_name = '{0}'", tableName);

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var uqName = reader.GetString(0);

                    var uq = uqs.FirstOrDefault(k => k.ConstraintName == uqName);
                    if (uq == null)
                    {
                        uq = new ConstraintInfo(tableName, uqName, ConstraintType.UQ);
                        uqs.Add(uq);
                    }

                    uq.Fields.Add(reader.GetString(1));
                }
            });

            return uqs;
        }

        /// inheritedDoc
        public async Task<List<SchemaInfo>> GetSchemasAsync()
        {
            var schemas = new List<SchemaInfo>();

            var sql = string.Format(
@"select 
	s.name as schema_name, 
    s.schema_id,
    u.name as schema_owner
from sys.schemas s
    inner join sys.sysusers u
    on u.uid = s.principal_id
order by s.name");

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    //var id = reader.GetInt32(1);
                    var owner = reader.GetString(2);

                    schemas.Add(new SchemaInfo(name, owner));
                }
            });

            return schemas;
        }

        /// inheritedDoc
        public async Task<List<TableColumn>> GetTableColumnsAsync(string schema, string table)
        {
            var columns = new List<TableColumn>();
            var sql =
$@"SELECT 
	COLUMN_NAME,
	IS_NULLABLE,
	DATA_TYPE,
	NUMERIC_PRECISION, -- precision (decimal, tinyint)
	NUMERIC_SCALE, -- scale (decimal, tinyint)
	CHARACTER_MAXIMUM_LENGTH-- max string length
FROM 
	INFORMATION_SCHEMA.COLUMNS c
WHERE 
	TABLE_NAME = '{table}'
	and TABLE_SCHEMA = '{schema}'
order by ORDINAL_POSITION";

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var isNullable = reader.GetString(1);
                    var dataType = reader.GetString(2);
                    
                    var precision = reader.GetNullableByte(3);
                    var scale = reader.GetNullableInt(4);
                    var maxLength = reader.GetNullableInt(5);

                    var dbType = DbTypesMap[dataType.ToLower()];

                    columns.Add(new TableColumn(name) { 
                        DataType = dbType,
                        DataTypeDefinition = dataType,
                        IsNullable = isNullable?.ToLower() == "yes",
                        Size = maxLength ?? precision,
                        Precision = scale,
                    });
                }
            });

            return columns;
        }

        public Task CreateTableAsync(string schema, string table, List<TableColumn> columns)
        {
            throw new NotImplementedException();
        }

        public string GetColumnDataTypeDefinition(TableColumn column)
        {
            throw new NotImplementedException();
        }

        public Task DropTableIfExists(string schema, string table)
        {
            throw new NotImplementedException();
        }

        public Task CreateSchemaAsync(string name)
        {
            throw new NotImplementedException();
        }

        private Dictionary<string, DbType> _reservedTypeMap;
        public Dictionary<string, DbType> DbTypesMap
        {
            get 
            {
                return _reservedTypeMap ?? (_reservedTypeMap = TypeMapHelper.GetReversedTypesMap(_typeMap , (typeName, dbTypes) => dbTypes.FirstOrDefault()));
            }
        }
    }
}