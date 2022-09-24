namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Schema info
    /// </summary>
    public class SchemaInfo
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Owner
        /// </summary>
        public string? Owner { get;set; }

        public SchemaInfo(string name, string? owner)
        {
            Name = name;
            Owner = owner;
        }
    }
}
