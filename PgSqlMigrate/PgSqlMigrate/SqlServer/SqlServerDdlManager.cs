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
    public class SqlServerDdlManager : DdlManagerBase, IDdlReader
    {
        public SqlServerDdlManager(DbContext dbContext) : base(dbContext, new SqlServer2008TypeMap())
        {
        }

        public async Task<List<string>> GetTableNamesAsync(string schemaName)
        {
            var tables = new List<string>();

            var sql = $@"SELECT
  	TABLE_NAME
FROM
  	INFORMATION_SCHEMA.TABLES 
where
	TABLE_SCHEMA = '{schemaName}'
	and TABLE_TYPE = 'BASE TABLE'";

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
        public async Task<List<ConstraintInfo>> RetrieveDefaultConstraintsAsync(string schema, string tableName, string columnName)
        {
            var defaults = new List<ConstraintInfo>();

            var sql = $@"
select 
	d.name, 
	d.definition
from 
	sys.tables t
    join sys.default_constraints d on d.parent_object_id = t.object_id
    join sys.columns c on c.object_id = t.object_id and c.column_id = d.parent_column_id
where 
	t.name = '{tableName}'
	and c.name = '{columnName}'
	and SCHEMA_NAME(t.schema_id) = '{schema}'";

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var defName = reader.GetString(0);

                    var uq = defaults.FirstOrDefault(k => k.ConstraintName == defName);
                    if (uq == null)
                    {
                        uq = new ConstraintInfo(schema, tableName, defName, ConstraintType.Default);
                        uq.Definition = reader.GetString(1);
                        defaults.Add(uq);
                    }

                    uq.Fields.Add(columnName);
                }
            });

            return defaults;
        }

        /// inhgeritDoc
        public async Task<List<IndexInfo>> RetrieveIndexesAsync(string schema, string tableName)
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
where 
	t.name = '{tableName}'
	and SCHEMA_NAME(t.schema_id) = '{schema}'";

            var indexes = new List<IndexInfo>();
            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var idxName = reader.GetString(1);

                    var idx = indexes.FirstOrDefault(k => k.Name == idxName);
                    if (idx == null)
                    {
                        idx = new IndexInfo(schema, tableName, idxName);
                        indexes.Add(idx);
                    }

                    idx.Columns.Add(reader.GetString(2));
                }
            });
            return indexes;
        }

        public async Task<List<IndexInfo>> RetrieveIndexes(string schema) 
        {
            var sql =
$@"SELECT 
	schema_name(t.schema_id),
    TableName = t.name,
    IndexName = ind.name,
    ColumnName = col.name,
	ic.is_included_column
FROM 
     sys.indexes ind 
INNER JOIN 
     sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
INNER JOIN 
     sys.columns col ON ic.object_id = col.object_id and ic.column_id = col.column_id 
INNER JOIN 
     sys.tables t ON ind.object_id = t.object_id 
WHERE 
     ind.is_primary_key = 0 
     AND ind.is_unique = 0 
     AND ind.is_unique_constraint = 0 
     AND t.is_ms_shipped = 0 
	 AND schema_name(t.schema_id) = '{schema}'
ORDER BY 
     t.name, ind.name, ind.index_id, ic.is_included_column, ic.key_ordinal";

            var indexes = new List<IndexInfo>();
            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var schemaName = reader.GetString(0);
                    var tableName = reader.GetString(1);
                    var indexName = reader.GetString(2);
                    var columnName = reader.GetString(3);
                    var isIncluded = reader.GetBoolean(4);

                    var idx = indexes.FirstOrDefault(i => i.Name == indexName && i.Table == tableName);
                    if (idx == null)
                    {
                        idx = new IndexInfo(schema, tableName, indexName);
                        indexes.Add(idx);
                    }

                    if (isIncluded)
                        idx.IncludedColumns.Add(columnName);
                    else
                        idx.Columns.Add(columnName);
                }
            });
            return indexes;
        }


        /// inhgeritDoc
        public async Task<List<ConstraintInfo>> RetrieveUniqueKeysAsync(string schema, string tableName)
        {
            var uqs = new List<ConstraintInfo>();

            var sql = 
$@"select 
	tc.CONSTRAINT_NAME,
	cu.COLUMN_NAME
from 
	information_schema.TABLE_CONSTRAINTS tc 
	left join information_schema.CONSTRAINT_COLUMN_USAGE cu on cu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
where 
	tc.CONSTRAINT_TYPE = 'UNIQUE'
	and tc.table_name = '{tableName}'
	and tc.TABLE_SCHEMA = '{schema}'";

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var uqName = reader.GetString(0);

                    var uq = uqs.FirstOrDefault(k => k.ConstraintName == uqName);
                    if (uq == null)
                    {
                        uq = new ConstraintInfo(schema, tableName, uqName, ConstraintType.UQ);
                        uqs.Add(uq);
                    }

                    uq.Fields.Add(reader.GetString(1));
                }
            });

            return uqs;
        }

        /// inhgeritDoc
        public async Task<List<ConstraintInfo>> RetrieveForeignKeysAsync(string schema, string tableName)
        {
            var fks = new List<ConstraintInfo>();

            var sql =
$@"SELECT  
	obj.name AS FK_NAME,
    col1.name AS [column],
    tab2.name AS [referenced_table],
	sch2.name as [referenced_table_schema],
    col2.name AS [referenced_column],
	rc.UPDATE_RULE,
	rc.DELETE_RULE
FROM 
	sys.foreign_key_columns fkc
	INNER JOIN sys.objects obj ON obj.object_id = fkc.constraint_object_id
	INNER JOIN information_schema.REFERENTIAL_CONSTRAINTS rc on rc.CONSTRAINT_NAME = obj.name
	INNER JOIN sys.tables tab1 ON tab1.object_id = fkc.parent_object_id
	INNER JOIN sys.schemas sch1 ON tab1.schema_id = sch1.schema_id
	INNER JOIN sys.columns col1 ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
	INNER JOIN sys.tables tab2 ON tab2.object_id = fkc.referenced_object_id
	INNER JOIN sys.columns col2 ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id
	INNER JOIN sys.schemas sch2 ON tab2.schema_id = sch2.schema_id
where
	tab1.name = '{tableName}'
	and sch1.name = '{schema}'";

            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var column = reader.GetString(1);
                    var refTable = reader.GetString(2);
                    var refTableSchema = reader.GetString(3);
                    var refColumn = reader.GetString(4);
                    var updateRule = reader.GetString(5);
                    var deleteRule = reader.GetString(6);

                    var fk = fks.FirstOrDefault(k => k.ConstraintName == name);
                    if (fk == null)
                    {
                        fk = new ConstraintInfo(schema, tableName, name, ConstraintType.FK);
                        fk.PrimaryTableName = refTable;
                        fk.PrimaryTableSchemaName = refTableSchema;
                        fk.PrimaryTableFields = new List<string> { refColumn };
                        fk.UpdateRule = updateRule;
                        fk.DeleteRule = deleteRule;
                        fks.Add(fk);
                    }

                    fk.Fields.Add(column);
                }
            });

            return fks;
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

                    var typeDefinition = dataType.ToLower();
                    var dbType = DbTypesMap.ContainsKey(typeDefinition)
                        ? DbTypesMap[typeDefinition]
                        : (DbType?)null;

                    columns.Add(new TableColumn(name) { 
                        DataType = dbType,
                        DataTypeDefinition = dataType,
                        IsUserDefinedType = dbType == null,
                        IsNullable = isNullable?.ToLower() == "yes",
                        Size = maxLength ?? precision,
                        Precision = scale,
                    });
                }
            });

            return columns;
        }

        public async Task<ConstraintInfo?> RetrievePrimaryKeyDefinition(string schema, string tableName)
        {
            var sql =
$@"select 
	cu.CONSTRAINT_NAME,
	cu.COLUMN_NAME
from 
	information_schema.TABLE_CONSTRAINTS tc 
	left join information_schema.CONSTRAINT_COLUMN_USAGE cu on cu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
where 
	tc.constraint_type = 'PRIMARY KEY' 
	and tc.TABLE_NAME = '{tableName}'
	and tc.TABLE_SCHEMA = '{schema}'";

            ConstraintInfo? pk = null;
            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var column = reader.GetString(1);

                    pk = pk ?? new ConstraintInfo(schema, tableName, reader.GetString(0), ConstraintType.PK);
                    pk.Fields.Add(reader.GetString(1));
                }
            });

            return pk;
        }

        public async Task<List<SqlModuleDefinition>> GetViewDefinitionsAsync(string schemaName)
        {
            return await GetScalarModulesDefinitionsAsync(schemaName, "V", SqlModuleType.View);
        }

        private async Task<List<SqlModuleDefinition>> GetScalarModulesDefinitionsAsync(string schemaName, string type, SqlModuleType sqlModuleType) 
        {
            var sql =
$@"SELECT 
	o.name,
	m.definition
FROM 
	sys.objects o
	INNER JOIN sys.sql_modules m ON o.object_id = m.object_id
WHERE 
	SCHEMA_NAME(o.schema_id) = '{schemaName}'
	and type = '{type}'
order by o.name";


            var items = new List<SqlModuleDefinition>();
            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    var definition = reader.GetString(1);

                    items.Add(new SqlModuleDefinition(schemaName, name, definition, sqlModuleType));
                }
            });

            return items;
        }

        public async Task<List<SqlModuleDefinition>> GetScalarFunctionDefinitionsAsync(string schemaName)
        {
            return await GetScalarModulesDefinitionsAsync(schemaName, "FN", SqlModuleType.ScalarFunction);
        }

        public async Task<List<SqlModuleDefinition>> GetStoredProcedureDefinitionsAsync(string schemaName)
        {
            return await GetScalarModulesDefinitionsAsync(schemaName, "P", SqlModuleType.StoredProcedure);
        }

        public async Task<List<SqlModuleDefinition>> GetTableFunctionDefinitionsAsync(string schemaName)
        {
            return await GetScalarModulesDefinitionsAsync(schemaName, "TF", SqlModuleType.TableValuedFunction);
        }

        public async Task<List<SqlModuleDefinition>> GetInlineTableFunctionDefinitionsAsync(string schemaName)
        {
            return await GetScalarModulesDefinitionsAsync(schemaName, "IF", SqlModuleType.InlineTableValuedFunction);
        }

        public async Task<bool> IsSchemaEmpty(string schemaName)
        {
            var sql = $@"SELECT cast((case when not exists (select 1 from sys.objects WHERE schema_id = SCHEMA_ID('{schemaName}')) then 1 else 0 end) as bit)";
            return (bool?)await ExecuteScalarAsync(sql) == true;
        }

        public async Task<List<IdentityColumnInfo>> GetTableAutoincrementColumnsAsync(string schema, string table)
        {
            var sql =
$@"SELECT 
	identity_columns.name as ColumnName,
	identity_columns.seed_value,
	identity_columns.increment_value,
	identity_columns.last_value
FROM 
	sys.tables tables 
	JOIN sys.identity_columns identity_columns on tables.object_id=identity_columns.object_id
where
	OBJECT_SCHEMA_NAME(tables.object_id, db_id()) = '{schema}'
	and tables.name = '{table}'";

            var items = new List<IdentityColumnInfo>();
            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var column = reader.GetString(0);
                    var seedValue = Convert.ToInt64(reader.GetValue(1));
                    var seedIncrement = Convert.ToInt64(reader.GetValue(2));
                    var lastValue = reader.IsDBNull(3)
                        ? (Int64?)null
                        : Convert.ToInt64(reader.GetValue(3));

                    items.Add(new IdentityColumnInfo(
                        schema, 
                        table,
                        column,
                        seedValue,
                        seedIncrement,
                        lastValue
                    ));
                }
            });

            return items;
        }

        public async Task<List<StatisticsInfo>> RetrieveAutoCreatedStatistics(string schema)
        {
            var sql =
$@"SELECT
    schema_name(t.schema_id),
	t.name AS TableName,
	c.name AS ColumnName,
	s.name AS StatName,
	STATS_DATE(s.[object_id], s.stats_id) AS LastUpdated,
	s.has_filter,
	s.filter_definition,
	s.auto_created,
	s.user_created,
	s.no_recompute
FROM 
	sys.stats s
	JOIN sys.stats_columns sc ON sc.[object_id] = s.[object_id] AND sc.stats_id = s.stats_id
	JOIN sys.columns c ON c.[object_id] = sc.[object_id] AND c.column_id = sc.column_id
	JOIN sys.tables t ON s.[object_id] = t.[object_id]
WHERE 
	t.is_ms_shipped = 0
	AND s.auto_created = 1 
	and schema_name(t.schema_id) = '{schema}'
ORDER BY t.name,s.name,c.name";

            var statistics = new List<StatisticsInfo>();
            await ExecuteQueryAsync(sql, reader => {
                while (reader.Read())
                {
                    var schemaName = reader.GetString(0);
                    var tableName = reader.GetString(1);
                    var columnName = reader.GetString(2);
                    var indexName = reader.GetString(3);
                    
                    var stat = statistics.FirstOrDefault(i => i.Name == indexName && i.Table == tableName);
                    if (stat == null)
                    {
                        stat = new StatisticsInfo(schema, tableName, indexName);
                        statistics.Add(stat);
                    }

                    stat.Columns.Add(columnName);
                }
            });

            return statistics;
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