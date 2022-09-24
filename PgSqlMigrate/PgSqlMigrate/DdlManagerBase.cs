using Microsoft.EntityFrameworkCore;
using PgSqlMigrate.TypeMaps;
using PgSqlMigrate.Utils;
using System.Data;
using System.Data.Common;

namespace PgSqlMigrate
{
    /// <summary>
    /// Base class of DDL manager
    /// </summary>
    public abstract class DdlManagerBase
    {
        private readonly DbContext _dbContext;
        private DbConnection? _dbConnection;
        protected ITypeMap _typeMap;

        public DdlManagerBase(DbContext dbContext, ITypeMap typeMap)
        {
            _dbContext = dbContext;
            _typeMap = typeMap;
        }

        public async Task<IDisposable> OpenConnectionAsync() 
        {
            if (_dbConnection != null)
                throw new Exception("Connection already created");

            _dbConnection = _dbContext.Database.GetDbConnection();
            await _dbConnection.OpenAsync();

            return new DisposeAction(() => { 
                _dbConnection.Close();
                _dbConnection = null;
            });
        }

        public async Task ExecuteQueryAsync(string sql, Func<IDataReader, Task> action)
        {
            using (var query = Connection.CreateCommand())
            {
                query.CommandText = sql;
                using (var reader = await query.ExecuteReaderAsync())
                {
                    await action.Invoke(reader);
                }
            }
        }

        public async Task ExecuteQueryAsync(string sql, Action<IDataReader> action)
        {
            using (var query = Connection.CreateCommand())
            {
                query.CommandText = sql;
                using (var reader = await query.ExecuteReaderAsync())
                {
                    action.Invoke(reader);
                }
            }
        }

        public async Task<object?> ExecuteScalarAsync(string sql)
        {
            using (var query = Connection.CreateCommand())
            {
                query.CommandText = sql;
                return await query.ExecuteScalarAsync();
            }
        }

        public async Task ExecuteNonQueryAsync(string sql, int? commandTimeout = null)
        {
            using (var query = Connection.CreateCommand())
            {
                query.CommandText = sql;
                if (commandTimeout.HasValue)
                    query.CommandTimeout = commandTimeout.Value;
                await query.ExecuteNonQueryAsync();
            }
        }

        protected DbConnection Connection 
        {
            get 
            {
                if (_dbConnection == null)
                    throw new Exception("DB connection is not active");

                return _dbConnection;
            }
        }

        public string GetFullName(string schema, string objectName)
        {
            return string.IsNullOrWhiteSpace(schema)
                ? @$"""{objectName}"""
                : @$"""{schema}"".""{objectName}""";
        }

        public DbCommand CreateQuery(string sql)
        {
            var query = Connection.CreateCommand();
            query.CommandText = sql;
            return query;
        }

        public DbTransaction BeginTransaction() 
        {
            return Connection.BeginTransaction();
        }
    }
}
