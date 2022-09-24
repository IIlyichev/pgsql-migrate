using Microsoft.EntityFrameworkCore;

namespace PgSqlMigrate
{
    public class SqlContext: DbContext
    {
        private readonly string _connectionString;
        public SqlContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
}
