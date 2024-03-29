﻿namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Index info
    /// </summary>
    public class IndexInfo
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Name { get; set; }
        public List<string> Columns { get; set; }
        public List<string> IncludedColumns { get; set; }

        public IndexInfo(string schema, string table, string name)
        {
            Schema = schema;
            Table = table;
            Name = name;
            Columns = new List<string>();
            IncludedColumns = new List<string>();
        }
    }
}
