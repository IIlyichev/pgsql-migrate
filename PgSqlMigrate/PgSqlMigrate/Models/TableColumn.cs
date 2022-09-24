using System.Data;

namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Table column
    /// </summary>
    public class TableColumn
    {
        public string Name { get; set; }
        public bool IsNullable { get; set; }
        public string? DataTypeDefinition { get; set; }
        public DbType DataType { get; set; }
        /*
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        */
        public int? Size { get; set; }
        public int? Precision { get; set; }

        public TableColumn(string name)
        {
            Name = name;
        }
    }
}
