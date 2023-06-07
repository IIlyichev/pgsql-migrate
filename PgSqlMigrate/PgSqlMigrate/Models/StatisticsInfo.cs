namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Statistics info
    /// </summary>
    public class StatisticsInfo
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Name { get; set; }
        public List<string> Columns { get; set; }

        public StatisticsInfo(string schema, string table, string name)
        {
            Schema = schema;
            Table = table;
            Name = name;
            Columns = new List<string>();
        }
    }
}
