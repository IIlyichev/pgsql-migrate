using Microsoft.EntityFrameworkCore;

namespace PgSqlMigrate
{
    public class PgSqlContext: DbContext
    {
        private readonly string _connectionString;
        public PgSqlContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }
    }
}
