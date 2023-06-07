using log4net;
using log4net.Config;
using NDesk.Options;
using PgSqlMigrate.DbObjectsRenaming;
using PgSqlMigrate.Models;
using PgSqlMigrate.PostgreSql;
using PgSqlMigrate.SqlServer;
using System.Text;

namespace PgSqlMigrate
{
    internal class Program
    {
        private static ILog _log;

        static async Task Main(string[] args)
        {
            XmlConfigurator.Configure();
            _log = LogManager.GetLogger("AppLog");

            Log("MS SQL 2 PostgreSql migration app");

            var srcConnectionString = string.Empty;
            var dstConnectionString = string.Empty;
            var useCiText = false;
            var schemaOnly = false;
            var tablesToSkip = new List<string>();
            var p = new OptionSet() {
    { "src=",
        "Source connection string (MS Sql Server)",
        v => srcConnectionString = v
    },
    { "dst=",
        "Destination connection string (PostgreSql Server)",
        v => dstConnectionString = v
    },
    { "skipTablesData=",
        "Comma-delimited list of tables to skip data migration",
        v => tablesToSkip = (v ?? "").Split(',').Select(t => t.Trim()).ToList()
    },
    {
        "useCiText=",
        "Use citext for all string types",
        v => useCiText = !string.IsNullOrWhiteSpace(v) && v.Equals("true", StringComparison.InvariantCultureIgnoreCase)
    },
    {
        "schemaOnly=",
        "Migrate schema only",
        v => schemaOnly = !string.IsNullOrWhiteSpace(v) && v.Equals("true", StringComparison.InvariantCultureIgnoreCase)
    },
};

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Log("Arguments are incorrect", e);
                return;
            }
            if (string.IsNullOrWhiteSpace(srcConnectionString) || string.IsNullOrWhiteSpace(dstConnectionString))
            {
                Log("Both source and destination connection strings are required");
                return;
            }
            Log($"useCiText = {useCiText}");
            Log($"schemaOnly = {schemaOnly}");

            //IDbObjectRenamingProvider renamingProvider = new SnakeCaseRenamingProvider();
            IDbObjectRenamingProvider renamingProvider = new AsIsRenamingProvider();

            var renamingResults = new List<ObjectRenamingResult>();
            renamingProvider.OnRenamed = (schemaName, objectType, oldName, newName, normalized) =>
            {
                var existingItem = renamingResults.FirstOrDefault(r => r.SchemaName == schemaName && r.ObjectType == objectType && r.OldName == oldName);
                if (existingItem != null)
                {
                    if (existingItem.NewName != newName)
                        throw new Exception($"Ambiguous renaming of {objectType} '{oldName}' in schema '{schemaName}'. Dictionary already contains new name '{existingItem.NewName}' but last renaming returned '{newName}'");
                }
                else
                    renamingResults.Add(new ObjectRenamingResult(schemaName, objectType, oldName, newName, normalized));
            };

            var debug = new List<string>();

            try
            {
                using (var dstDb = new PgSqlContext(dstConnectionString))
                {
                    using (var srcDb = new SqlContext(srcConnectionString))
                    {
                        IDdlReader sqlManager = new SqlServerDdlManager(srcDb);
                        IDdlWriter pgManager = new PostgreSqlDdlManager(dstDb, useCiText);

                        Log("Try to connect to SQL Server...");
                        using (await sqlManager.OpenConnectionAsync())
                        {
                            Log("Connected to SQL Server successfully");

                            Log("Try to connect to PostgreSql Server...");
                            using (await pgManager.OpenConnectionAsync())
                            {
                                Log("Connected to PostgreSql successfully");

                                // install prerequisites
                                await pgManager.ExecuteNonQueryAsync("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");
                                if (useCiText)
                                    await pgManager.ExecuteNonQueryAsync("CREATE EXTENSION IF NOT EXISTS \"citext\"");

                                var schemas = await sqlManager.GetSchemasAsync();
                                
                                foreach (var schema in schemas)
                                {
                                    if (await sqlManager.IsSchemaEmpty(schema.Name))
                                    {
                                        Log($"Schema '{schema.Name}' is empty - skipped");
                                        continue;
                                    }
                                    
                                    Log($"### Schema: {schema.Name}");

                                    await pgManager.CreateSchemaAsync(schema.Name);

                                    Log("Get tables...");
                                    var tables = await sqlManager.GetTableNamesAsync(schema.Name);
                                    Log($"found {tables.Count} tables");

                                    foreach (var table in tables)
                                    {
                                        //if (table != "Core_Sites")
                                        //    continue;

                                        Log($"{table}");
                                        Log($"  try to get columns");
                                        var cols = await sqlManager.GetTableColumnsAsync(schema.Name, table);
                                        Log($"  found {cols.Count()} columns");

                                        Log($"Create table on PostgreSQL");

                                        var dstTableName = renamingProvider.GetTableName(schema.Name, table);
                                        var dstCols = GetRenamedColumns(cols, renamingProvider);

                                        await pgManager.CreateTableAsync(schema.Name, dstTableName, dstCols);

                                        // flow data
                                        if (!schemaOnly) 
                                        {
                                            var skipData = tablesToSkip != null && tablesToSkip.Contains($"{schema.Name}.{table}");
                                            if (!skipData)
                                            {
                                                Log($"Copy table data");
                                                await FlowTableDataAsync(sqlManager, pgManager, schema.Name, table, cols, renamingProvider);
                                                Log($"Copy table data - success");
                                            }
                                            else
                                                Log($"Copy table data skipped");
                                        }

                                        // get PK
                                        Log($"Create primary key");
                                        var pkConstraint = await sqlManager.RetrievePrimaryKeyDefinition(schema.Name, table);
                                        if (pkConstraint != null)
                                        {
                                            // create PK
                                            var dstPk = renamingProvider.ConvertConstraint(pkConstraint);
                                            await pgManager.CreatePrimaryKeyAsync(dstPk);
                                            Log($"Primary key created");
                                        }
                                        else
                                            Log($"Primary key not found - skipped");

                                        // create unique constraints
                                        Log($"Create unique constraints");
                                        var uqConstraints = await sqlManager.RetrieveUniqueKeysAsync(schema.Name, table);
                                        if (uqConstraints.Any())
                                        {
                                            foreach (var uq in uqConstraints)
                                            {
                                                var dstUq = renamingProvider.ConvertConstraint(uq);
                                                Log($"  {dstUq.ConstraintName}");
                                                await pgManager.CreateUniqueKeyAsync(dstUq);
                                            }
                                            Log($"Unique constraints created");
                                        }
                                        else
                                            Log($"Unique constraints not found - skipped");


                                        // create identities
                                        Log($"Check autoincrement columns");
                                        var autoincrementColumns = await sqlManager.GetTableAutoincrementColumnsAsync(schema.Name, table);
                                        if (autoincrementColumns.Any()) 
                                        {
                                            Log($"Autoincrement columns found");
                                            foreach (var identity in autoincrementColumns)
                                            {
                                                Log($"Make '{identity.ColumnName}' column autoincrement");
                                                
                                                var dstIdentity = renamingProvider.ConvertAutoincrementColumn(identity);
                                                await pgManager.MakeColumnAutoincrementAsync(dstIdentity);
                                            }
                                        }                                        

                                        // create default constraints
                                        Log($"Create defaults");
                                        var defaultsFound = false;
                                        foreach (var column in cols)
                                        {
                                            var defaultConstraints = await sqlManager.RetrieveDefaultConstraintsAsync(schema.Name, table, column.Name);
                                            defaultsFound = defaultsFound || defaultConstraints.Any();
                                            foreach (var defaultConstraint in defaultConstraints)
                                            {
                                                defaultConstraint.ValueType = column.DataType;

                                                var dstConstraint = renamingProvider.ConvertConstraint(defaultConstraint);

                                                Log($"Create constraint '{defaultConstraint.ConstraintName}'");
                                                await pgManager.CreateDefaultConstraintAsync(dstConstraint);
                                            }
                                        }
                                        Log(defaultsFound ? "Defaults created" : "Defaults not found - skipped");
                                    }
                                }
                                
                                foreach (var schema in schemas)
                                {
                                    Log($"SCHEMA: '{schema.Name}'");
                                    var tables = await sqlManager.GetTableNamesAsync(schema.Name);

                                    Log($"Create foreign keys");
                                    foreach (var table in tables)
                                    {
                                        Log($"    {table}");
                                        // create foreign keys
                                        var fkConstraints = await sqlManager.RetrieveForeignKeysAsync(schema.Name, table);
                                        Log($"  found {fkConstraints.Count()} foreign keys");
                                        var fkNo = 1;
                                        foreach (var fk in fkConstraints)
                                        {
                                            var dstFk = renamingProvider.ConvertConstraint(fk);

                                            Log($"      {fkNo}/{fkConstraints.Count()}: {dstFk.ConstraintName}");
                                            await pgManager.CreateForeignKeyAsync(dstFk);
                                            Log($"      {fkNo}/{fkConstraints.Count()}: {dstFk.ConstraintName} - created");
                                            fkNo++;
                                        }
                                    }
                                    Log($"Foreign keys created successfully");

                                    // Create indexes
                                    // note: index names are unique in the entire schema
                                    Log($"Create indexes");
                                    var indexes = await sqlManager.RetrieveIndexes(schema.Name);
                                    if (indexes.Any())
                                    {
                                        var dstIndexes = ConvertIndexes(indexes, renamingProvider);

                                        Log($"found {indexes.Count()} indexes");
                                        var idxNo = 1;
                                        foreach (var index in dstIndexes)
                                        {
                                            Log($"  {idxNo}/{indexes.Count()}: {index.Name}");

                                            await pgManager.CreateIndexAsync(index);

                                            Log($"  {idxNo}/{indexes.Count()}: {index.Name} - created");
                                            idxNo++;
                                        }
                                        Log($"Indexes successfully created");
                                    }
                                    else
                                    {
                                        Log($"Indexes not found");
                                    }

                                    Log($"Create indexes for auto-created statistics");
                                    var statistics = await sqlManager.RetrieveAutoCreatedStatistics(schema.Name);
                                    if (statistics.Any())
                                    {
                                        Log($"found {statistics.Count()} auto-created statistics");

                                        var dstIndexes = ConvertStatisticsToIndexes(statistics, renamingProvider);
                                        
                                        var idxNo = 1;
                                        foreach (var index in dstIndexes)
                                        {
                                            Log($"  {idxNo}/{statistics.Count()}: {index.Name}");

                                            await pgManager.CreateIndexAsync(index);

                                            Log($"  {idxNo}/{statistics.Count()}: {index.Name} - created");
                                            idxNo++;
                                        }
                                        Log($"Indexes for auto-created statistics successfully created");
                                    }
                                    else
                                    {
                                        Log($"Indexes for auto-created statistics not found");
                                    }
                                }

                                //File.WriteAllLines(@"d:\temp\NonSnakeCaseNames.csv", renamingProvider.NonSnakeCaseNames);
                                //File.WriteAllLines(@"d:\temp\MissingNames.csv", renamingProvider.MissingNames);
                                File.WriteAllLines(@"d:\temp\debug.txt", debug);
                            }
                        }
                    }
                }

                Log($"Process finished");
                var orderedReport = renamingResults
                    .OrderBy(r => r.SchemaName).ThenBy(r => r.ObjectType).ThenBy(r => r.OldName)
                    .Select(r => $"{r.SchemaName};{r.ObjectType};{r.OldName};{r.NewName};{r.Normalized};")
                    .ToList();
                orderedReport.Insert(0, "Schema;Object Type;Old Name;New Name;Is Normalized;");
                var reportFileName = Path.Combine(AppContext.BaseDirectory, $"Renaming-{DateTime.Now:yyyyMMdd-hhmm}.csv");
                await File.WriteAllLinesAsync(reportFileName, orderedReport);
                
                Log($"Report generated: {reportFileName}");
            }
            catch (Exception e) 
            {
                Log("Failed to migrate", e);
                throw;
            }
        }

        static void Log(string message, Exception? e = null) 
        {
            //Console.WriteLine(message);
            if (e != null) {
                //Console.WriteLine("ERROR: " + e.Message);
                _log.Error(message, e);
            } else
                _log.Info(message);
        }

        static List<IndexInfo> ConvertIndexes(List<IndexInfo> indexes, IDbObjectRenamingProvider renamingProvider)
        {
            var convertedIndexes = indexes.Select(index => renamingProvider.ConvertIndex(index)).ToList();

            var duplicates = convertedIndexes.GroupBy(index => index.Name, (name, items) => new { Name = name, Items = items })
                .Where(g => g.Items.Count() > 1)
                .ToList();

            foreach (var group in duplicates)
            {
                var idxNo = 1;
                foreach (var index in group.Items)
                {
                    index.Name = $"{index.Name}{idxNo++}";
                }
            }
            return convertedIndexes;
        }

        static List<IndexInfo> ConvertStatisticsToIndexes(List<StatisticsInfo> statisics, IDbObjectRenamingProvider renamingProvider)
        {
            var convertedIndexes = statisics.Select(stat => {
                var indexInfo = new IndexInfo(stat.Schema, stat.Table, stat.Name) { Columns = stat.Columns };                    
                return renamingProvider.ConvertIndex(indexInfo);
            }).ToList();

            var duplicates = convertedIndexes.GroupBy(index => index.Name, (name, items) => new { Name = name, Items = items })
                .Where(g => g.Items.Count() > 1)
                .ToList();

            foreach (var group in duplicates)
            {
                var idxNo = 1;
                foreach (var index in group.Items)
                {
                    index.Name = $"{index.Name}{idxNo++}";
                }
            }
            return convertedIndexes;
        }


        static async Task FlowTableDataAsync(IDdlReader src, IDdlWriter dst, string schema, string table, List<TableColumn> columns, IDbObjectRenamingProvider renamingProvider)
            {
                var srcQuery = GenerateSelectQuery(src, schema, table, columns);

                var dstTableName = renamingProvider.GetTableName(schema, table);
                var dstColumns = GetRenamedColumns(columns, renamingProvider);

                var dstSql = GenerateInsertQuery(dst, schema, dstTableName, dstColumns);

                var totalRows = Convert.ToInt32(await src.ExecuteScalarAsync($"select count(1) from {src.GetFullName(schema, table)}"));

                var dstQuery = dst.CreateQuery(dstSql);
                for (var i = 0; i < dstColumns.Count; i++)
                {
                    var sqlParam = dstQuery.CreateParameter();
                    sqlParam.ParameterName = dstColumns[i].Name;
                    dstQuery.Parameters.Add(sqlParam);
                }

                var rowNo = 1;
                var startTime = DateTime.Now;
                var quantity = totalRows / 100;
                if (quantity == 0)
                    quantity = 1;
                if (quantity > 500)
                    quantity = 500;

                var transaction = dst.BeginTransaction();

                await src.ExecuteQueryAsync(srcQuery, async (reader) => {
                    try
                    {
                        while (reader.Read())
                        {
                            if (rowNo % quantity == 0)
                            {
                                var span = (DateTime.Now - startTime);
                                var speed = Math.Round(rowNo / (span.TotalSeconds > 0 ? span.TotalSeconds : 1), 2);
                                var avgSpeed = Convert.ToDecimal(speed);
                                var estimated = new TimeSpan(span.Ticks / rowNo * totalRows);
                                Log($"      processed {rowNo} from {totalRows} ({(double)rowNo / totalRows * 100:0.#}%), estimated time = {estimated.Minutes:D2}:{estimated.Seconds:D2}:{estimated.Milliseconds:D3}, speed = {speed} row/sec");
                                await transaction.CommitAsync();
                                transaction = dst.BeginTransaction();
                            }

                            rowNo++;
                            for (var i = 0; i < dstColumns.Count; i++)
                            {
                                var colValue = reader.GetValue(i);
                                dstQuery.Parameters[i].Value = colValue != null && colValue is string strValue
                                    ? strValue.Replace("\0", "") // remove null characters to prevent error `22021`
                                    : colValue;
                            }
                            dstQuery.CommandTimeout = 10 * 60;

                            await dstQuery.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex) 
                    {
                        throw;
                    }
                });

                await transaction.CommitAsync();
            }

        static string GenerateSelectQuery(IDdlManagerBase ddlManager, string schema, string table, List<TableColumn> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("select");
            for (int i = 0; i < columns.Count; i++)
            {
                sb.Append(@$"    ""{columns[i].Name}""");
                sb.AppendLine(i < columns.Count - 1 ? "," : "");
            }
            sb.AppendLine($"from {ddlManager.GetFullName(schema, table)}");

            return sb.ToString();
        }

        static string GenerateInsertQuery(IDdlManagerBase dst, string schema, string table, List<TableColumn> columns)
        {
            var sb = new StringBuilder();
            sb.AppendLine("insert into");
            sb.AppendLine($" {dst.GetFullName(schema, table)}");
            sb.AppendLine("     (");
            for (int i = 0; i < columns.Count; i++)
            {
                sb.Append(@$"    ""{columns[i].Name}""");
                sb.AppendLine(i < columns.Count - 1 ? "," : "");
            }
            sb.AppendLine("     )");
            sb.AppendLine("values");
            sb.AppendLine("     (");
            for (int i = 0; i < columns.Count; i++)
            {
                sb.Append(@$"    @{columns[i].Name}");
                sb.AppendLine(i < columns.Count - 1 ? "," : "");
            }
            sb.AppendLine("     )");

            return sb.ToString();
        }

        static List<TableColumn> GetRenamedColumns(List<TableColumn> columns, IDbObjectRenamingProvider renamingProvider)
        {
            return columns.Select(c => new TableColumn(c) { Name = renamingProvider.GetColumnName(c.Name) }).ToList();
        }
    }
}