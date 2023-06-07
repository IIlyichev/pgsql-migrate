using PgSqlMigrate.Models;

namespace PgSqlMigrate.SqlParsing
{
    /// <summary>
    /// Sql token replacer
    /// </summary>
    public interface ISqlTokenReplacer
    {
        /// <summary>
        /// Replace all tokens
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="replacements"></param>
        /// <returns></returns>
        string ReplaceAll(string sql, List<ObjectRenamingResult> replacements, List<string> schemaNames);
    }
}
