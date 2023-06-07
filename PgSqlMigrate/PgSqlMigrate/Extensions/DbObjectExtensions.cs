using PgSqlMigrate.Models;

namespace PgSqlMigrate.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class DbObjectExtensions
    {
        /// <summary>
        /// Convert ConstraintType to DbObjectType
        /// </summary>
        /// <param name="constraintType"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static DbObjectType ToDbObjectType(this ConstraintType constraintType) 
        {
            switch (constraintType) 
            {
                case ConstraintType.Default:
                    return DbObjectType.DefaultConstraint;
                case ConstraintType.UQ:
                    return DbObjectType.UniqueKey;
                case ConstraintType.PK:
                    return DbObjectType.PrimaryKey;
                case ConstraintType.FK:
                    return DbObjectType.ForeignKey;
                default:
                    throw new NotSupportedException($"{constraintType} is not supported");
            }
        }
    }
}
