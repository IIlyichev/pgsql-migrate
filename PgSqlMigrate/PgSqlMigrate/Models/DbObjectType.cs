namespace PgSqlMigrate.Models
{
    /// <summary>
    /// DB objct type
    /// </summary>
    public enum DbObjectType
    {
        Schema = 1,
        Table = 2,
        Index = 3,
        ForeignKey = 4,
        UniqueKey = 5,
        DefaultConstraint = 6,
        Function = 7,
        Procedure = 8,
        View = 9,
        Column = 10,
        PrimaryKey = 11,
    }
}
