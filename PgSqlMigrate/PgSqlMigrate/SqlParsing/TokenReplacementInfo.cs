namespace PgSqlMigrate.SqlParsing
{
    /// <summary>
    /// Sql token replacement info
    /// </summary>
    public class TokenReplacementInfo
    {
        public int BeginPosition { get; set; }
        public int EndPosition { get; set; }
        public string NewText { get; set; }

        public TokenReplacementInfo(int beginPosition, int endPosition, string newText)
        {
            BeginPosition = beginPosition;
            EndPosition = endPosition;
            NewText = newText;
        }
    }
}
