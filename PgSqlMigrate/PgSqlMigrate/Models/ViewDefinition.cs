namespace PgSqlMigrate.Models
{
    /// <summary>
    /// View definition
    /// </summary>
    public class ViewDefinition
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public string Definition { get; set; }

        public ViewDefinition(string schema, string name, string definition)
        {
            Schema = schema;
            Name = name;
            Definition = definition;
        }
    }
}
