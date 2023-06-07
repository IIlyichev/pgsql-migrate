namespace PgSqlMigrate.DbObjectsRenaming
{
    public class DbObjectRenamingModel
    {
        public string Schema { get; set; }
        public string OldName { get; set; }
        public string NewName { get; set; }
        public string Type { get; set; }

        public DbObjectRenamingModel(string type, string schema, string oldName, string newName)
        {
            Type = type;
            Schema = schema;
            OldName = oldName;
            NewName = newName;
        }
    }
}
