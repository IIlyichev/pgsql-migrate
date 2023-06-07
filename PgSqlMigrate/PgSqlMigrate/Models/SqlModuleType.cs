namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Sql module type
    /// </summary>
    public enum SqlModuleType
    {
        View,
        ScalarFunction,
        StoredProcedure,
        TableValuedFunction,
        InlineTableValuedFunction        
    }
}
