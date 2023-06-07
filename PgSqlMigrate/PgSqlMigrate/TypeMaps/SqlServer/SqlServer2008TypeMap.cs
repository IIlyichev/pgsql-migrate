using System.Data;

namespace PgSqlMigrate.TypeMaps.SqlServer
{
    internal class SqlServer2008TypeMap : SqlServer2005TypeMap
    {
        protected override void SetupTypeMaps()
        {
            base.SetupTypeMaps();

            SetTypeMap(DbType.DateTime2, "DATETIME2");
            SetTypeMap(DbType.DateTimeOffset, "DATETIMEOFFSET");
            SetTypeMap(DbType.DateTimeOffset, "DATETIMEOFFSET($size)", maxSize: 7);
            SetTypeMap(DbType.Date, "DATE");
            SetTypeMap(DbType.Time, "TIME");

            SetTypeMap(DbType.Double, "FLOAT");
        }
    }
}
