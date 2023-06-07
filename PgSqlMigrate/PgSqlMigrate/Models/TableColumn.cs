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
        public DbType? DataType { get; set; }
        public bool IsUserDefinedType { get; set; }
        public int? Size { get; set; }
        public int? Precision { get; set; }

        public TableColumn(string name)
        {
            Name = name;
        }

        public TableColumn(TableColumn sourceColumn)
        {
            Name = sourceColumn.Name;
            IsNullable = sourceColumn.IsNullable;
            DataTypeDefinition = sourceColumn.DataTypeDefinition;
            DataType = sourceColumn.DataType;
            Size = sourceColumn.Size;
            Precision = sourceColumn.Precision;
            IsUserDefinedType = sourceColumn.IsUserDefinedType;
        }
    }
}
