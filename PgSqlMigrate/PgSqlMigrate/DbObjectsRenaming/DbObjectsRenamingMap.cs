namespace PgSqlMigrate.DbObjectsRenaming
{
    public class DbObjectsRenamingMap : List<DbObjectRenamingModel>
    {
        public void Add(string type, string schema, string oldName, string newName)
        {
            Add(new DbObjectRenamingModel(type, schema, oldName, newName));
        }
    }
}
