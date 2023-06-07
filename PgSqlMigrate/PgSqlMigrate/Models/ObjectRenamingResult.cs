namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Renaming result, is used for reports
    /// </summary>
    public class ObjectRenamingResult
    {
        /// <summary>
        /// Schema name
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Object type
        /// </summary>
        public DbObjectType ObjectType { get; set; }

        /// <summary>
        /// Old name
        /// </summary>
        public string OldName { get; set; }

        /// <summary>
        /// New name
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// If true, indicates that the new name is normalized
        /// </summary>
        public bool Normalized { get; set; }
        
        public ObjectRenamingResult(string schemaName, DbObjectType objectType, string oldName, string newName, bool normalized)
        {
            SchemaName = schemaName;
            ObjectType = objectType;
            OldName = oldName;
            NewName = newName;
            Normalized = normalized;
        }
    }
}
