namespace PgSqlMigrate.Models
{
    /// <summary>
    /// Migration context
    /// </summary>
    public class MigrationContext
    {
        public string SrcConnectionString { get; set; }
        public string DstConnectionString { get; set; }
        public OnLog? OnLog { get; set; }

        public void Log(string message, Exception? e = null)
        {
            OnLog?.Invoke(message, e);
        }

        public MigrationContext(string srcConnectionString, string dstConnectionString)
        {
            SrcConnectionString = srcConnectionString;
            DstConnectionString = dstConnectionString;
        }
    }

    public delegate void OnLog(string message, Exception? e = null);
    public delegate Task DirectMigrateAsync(IDdlReader reader, IDdlWriter writer);
}
