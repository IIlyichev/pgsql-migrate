namespace PgSqlMigrate.DbObjectsRenaming
{
    public class MappingNotFoundException : Exception
    {
        public MappingNotFoundException(string objectType, string objectName): base($"Mapping for {objectType} with name `{objectName}` not found")
        {

        }
    }
}
