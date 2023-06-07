namespace PgSqlMigrate.DbObjectsRenaming
{
    public class AmbiguousMappingException: Exception
    {
        public AmbiguousMappingException(string objectType, string objectName): base($"Ambiguous mapping. Found multiple values for {objectType} with name `{objectName}`")
        {

        }
    }
}
