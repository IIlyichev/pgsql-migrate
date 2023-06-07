using System.Data;

namespace PgSqlMigrate.Models
{
    public class ConstraintInfo
    {
        public string Schema { get; set; }
        public string TableName { get; set; }
        public string ConstraintName { get; set; }
        public ConstraintType Type { get; set; }

        public List<string> Fields { get; set; }

        #region Foreign keys specific properties

        public string? PrimaryTableName { get; set; }
        public string? PrimaryTableSchemaName { get; set; }
        public List<string> PrimaryTableFields { get; set; }
        public string? UpdateRule { get; set; }
        public string? DeleteRule { get; set; }

        #endregion

        #region Default constraint specific properties

        public string? Definition { get; set; }
        public DbType? ValueType { get; set; }

        #endregion

        public ConstraintInfo(string schema, string tableName, string constraintName, ConstraintType type)
        {
            Schema = schema;
            TableName = tableName;
            ConstraintName = constraintName;
            Type = type;
            Fields = new List<string>();
            PrimaryTableFields = new List<string>();
        }
    }
}
