namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Identity column info
    /// </summary>
    public class IdentityColumnInfo
    {
        public string Schema { get; set; }
        public string Table { get; set; }

        public string ColumnName { get; set; }
        public Int64 SeedValue { get; set; }
        public Int64 SeedIncrement { get; set; }
        public Int64? LastValue { get; set; }

        public IdentityColumnInfo(
            string schema,
            string table,
            string columnName,
            Int64 seedValue,
            Int64 seedIncrement,
            Int64? lastValue)
        {
            Schema = schema;
            Table = table;
            ColumnName = columnName;
            SeedValue = seedValue;
            SeedIncrement = seedIncrement;
            LastValue = lastValue;
        }
    }
}
