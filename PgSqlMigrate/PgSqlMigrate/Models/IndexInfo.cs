namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Index info
    /// </summary>
    public class IndexInfo
    {
        public string Table { get; set; }
        public string Name { get; set; }
        public List<string> Fields { get; set; }

        public IndexInfo(string table, string name)
        {
            Table = table;
            Name = name;
            Fields = new List<string>();
        }
    }
}
