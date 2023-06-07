namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Scalar function definition
    /// </summary>
    public class SqlModuleDefinition
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public string Definition { get; set; }
        public SqlModuleType Type { get; set; }

        public SqlModuleDefinition(string schema, string name, string definition, SqlModuleType type)
        {
            Schema = schema;
            Name = name;
            Definition = definition;
            Type = type;
        }
    }
}
