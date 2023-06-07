using System.Data;
using System.Data.Common;

namespace PgSqlMigrate
{
    /// <summary>
    /// Base functionality of DDL manager
    /// </summary>
    public interface IDdlManagerBase
    {
        DbCommand CreateQuery(string sql);
        Task ExecuteQueryAsync(string sql, Func<IDataReader, Task> action);
        Task ExecuteQueryAsync(string sql, Action<IDataReader> action);
        Task<object?> ExecuteScalarAsync(string sql);
        Task ExecuteNonQueryAsync(string sql, int? commandTimeout = null);
        string GetFullName(string schema, string objectName);
        DbTransaction BeginTransaction();
        Task<IDisposable> OpenConnectionAsync();
    }
}
