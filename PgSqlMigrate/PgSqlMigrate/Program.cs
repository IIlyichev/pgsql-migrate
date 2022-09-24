// See https://aka.ms/new-console-template for more information
using NDesk.Options;
using PgSqlMigrate;
using PgSqlMigrate.Models;
using PgSqlMigrate.PostgreSql;
using PgSqlMigrate.SqlServer;
using System.Text;

Console.WriteLine("PgSql migration app");

var srcConnectionString = string.Empty;
var dstConnectionString = string.Empty;
var p = new OptionSet() {
    { "src=",
        "Source connection string (MS Sql Server)",
        v => srcConnectionString = v
    },
    { "dst=",
        "Destination connection string (PostgreSql Server)",
        v => dstConnectionString = v
    }
};

List<string> extra;
try
{
    extra = p.Parse(args);
}
catch (OptionException e)
{
    Console.WriteLine("Error: " + e.Message);
    return;
}
if (string.IsNullOrWhiteSpace(srcConnectionString) || string.IsNullOrWhiteSpace(dstConnectionString)) 
{
    Console.WriteLine("Both source and destination connection strings are required");
    return;
}

using (var dstDb = new PgSqlContext(dstConnectionString)) 
{
    using (var srcDb = new SqlContext(srcConnectionString))
    {
        var sqlManager = new SqlServerDdlManager(srcDb);
        var pgManager = new PostgreSqlDdlManager(dstDb);

        Console.Write("Try to connect to SQL Server...");
        using (await sqlManager.OpenConnectionAsync()) 
        {
            Console.WriteLine("connected");

            Console.Write("Try to connect to PostgreSql Server...");
            using (await pgManager.OpenConnectionAsync()) 
            {
                Console.WriteLine("connected");

                var schemas = await sqlManager.GetSchemasAsync();
                foreach (var schema in schemas) 
                {

                    Console.WriteLine($"### Schema: {schema}");

                    await pgManager.CreateSchemaAsync(schema.Name);

                    Console.Write("Get tables...");
                    var tables = await sqlManager.GetTableNamesAsync(schema.Name);
                    Console.WriteLine($"found {tables.Count} tables");


                    foreach (var table in tables)
                    {
                        Console.WriteLine($"    {table}");
                        Console.Write($"    try to get columns");
                        var cols = await sqlManager.GetTableColumnsAsync(schema.Name, table);
                        Console.WriteLine($" - ok, found {cols.Count()} columns");

                        Console.WriteLine($"Create table on PostgreSQL");

                        await pgManager.CreateTableAsync(schema.Name, table, cols);

                        // flow data
                        await FlowTableDataAsync(sqlManager, pgManager, schema.Name, table, cols);
                    }
                }
            }
        }
    }
}

async Task FlowTableDataAsync(IDdlManager src, IDdlManager dst, string schema, string table, List<TableColumn> columns)
{
    var srcQuery = GenerateSelectQuery(src, schema, table, columns);
    
    var dstSql = GenerateInsertQuery(dst, schema, table, columns);

    var totalRows = Convert.ToInt32(await src.ExecuteScalarAsync($"select count(1) from {dst.GetFullName(schema, table)}"));

    var dstQuery = dst.CreateQuery(dstSql);
    for (var i = 0; i < columns.Count; i++)
    {
        var sqlParam = dstQuery.CreateParameter();
        sqlParam.ParameterName = columns[i].Name;
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
        while (reader.Read()) 
        {
            if (rowNo % quantity == 0)
            {
                var span = (DateTime.Now - startTime);
                var speed = Math.Round(rowNo / (span.TotalSeconds > 0 ? span.TotalSeconds : 1), 2);
                var avgSpeed = Convert.ToDecimal(speed);
                var estimated = new TimeSpan(span.Ticks / rowNo * totalRows);
                Console.WriteLine($"      processed {(double)rowNo / totalRows * 100:0.#}% from {totalRows}, estimated time = {estimated.Minutes:D2}:{estimated.Seconds:D2}:{estimated.Milliseconds:D3}, speed = {speed} row/sec");
                await transaction.CommitAsync();
                transaction = dst.BeginTransaction();
            }

            rowNo++;
            for(var i = 0; i < columns.Count; i++)
            {
                var colValue = reader.GetValue(i);
                dstQuery.Parameters[i].Value = colValue;
            }
            dstQuery.CommandTimeout = 10 * 60;
            await dstQuery.ExecuteNonQueryAsync();
        }
    });
    
    await transaction.CommitAsync();
}

string GenerateSelectQuery(IDdlManager ddlManager, string schema, string table, List<TableColumn> columns)
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

string GenerateInsertQuery(IDdlManager dst, string schema, string table, List<TableColumn> columns)
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

Console.WriteLine("finished");