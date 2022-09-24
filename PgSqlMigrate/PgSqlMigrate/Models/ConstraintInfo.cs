namespace PgSqlMigrate.Models
{
    public class ConstraintInfo
    {
        public string TableName { get; set; }
        public string ConstraintName { get; set; }
        public ConstraintType Type { get; set; }

        public List<string> Fields { get; set; }

        #region Foreign keys specific properties

        public string? PrimaryTableName { get; set; }
        public List<string> PrimaryTableFields { get; set; }
        public string? UpdateRule { get; set; }
        public string? DeleteRule { get; set; }

        #endregion

        #region Default constraint specific properties

        public string? Definition { get; set; }

        #endregion

        public ConstraintInfo(string tableName, string constraintName, ConstraintType type)
        {
            TableName = tableName;
            ConstraintName = constraintName;
            Type = type;
            Fields = new List<string>();
            PrimaryTableFields = new List<string>();
        }
    }
}
