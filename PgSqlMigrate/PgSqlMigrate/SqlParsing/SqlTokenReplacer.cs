using PgSqlMigrate.DbObjectsRenaming;
using PgSqlMigrate.Extensions;
using PgSqlMigrate.Models;
using TSQL;
using TSQL.Tokens;

namespace PgSqlMigrate.SqlParsing
{
    public class SqlTokenReplacer: ISqlTokenReplacer
    {
        public string ReplaceAll(string sql, List<ObjectRenamingResult> replacements, List<string> schemaNames)
        {
            var tokens = TSQLTokenizer.ParseTokens(sql);
            var tokenReplacements = new List<TokenReplacementInfo>();
            TSQLToken prevToken = null;
            foreach (var token in tokens)
            {
                if (token is TSQLIdentifier sqlIdentifier)
                {
                    var oldName = sqlIdentifier.Name;
                    var newName = schemaNames.Contains(oldName)
                        ? oldName
                        : GetNewName(replacements, oldName);
                    tokenReplacements.Add(new TokenReplacementInfo(sqlIdentifier.BeginPosition, sqlIdentifier.EndPosition, newName));
                }

                if (token is TSQLStringLiteral literal && prevToken is TSQLKeyword keyword && keyword.Text == "as")
                {
                    tokenReplacements.Add(new TokenReplacementInfo(literal.BeginPosition, literal.EndPosition, "\"" + literal.Value + "\""));
                }

                prevToken = token;
            }

            var dstSql = "";
            var delta = 0;
            var prevPosition = 0;
            foreach (var tokenReplacement in tokenReplacements)
            {
                if (dstSql == "" && delta == 0)
                {
                    dstSql = sql.Substring(0, tokenReplacement.BeginPosition);
                    dstSql += tokenReplacement.NewText;

                    var oldLength = tokenReplacement.EndPosition - tokenReplacement.BeginPosition + 1;
                    delta += tokenReplacement.NewText.Length - oldLength;
                    prevPosition = tokenReplacement.EndPosition;
                }
                else
                {
                    var middleTextFirstChar = prevPosition + 1;
                    var middleText = sql.Substring(middleTextFirstChar, tokenReplacement.BeginPosition - middleTextFirstChar);
                    dstSql += middleText;
                    dstSql += tokenReplacement.NewText;

                    var oldLength = tokenReplacement.EndPosition - tokenReplacement.BeginPosition + 1;
                    delta += tokenReplacement.NewText.Length - oldLength;
                    prevPosition = tokenReplacement.EndPosition;
                }
            }

            if (prevPosition > 0)
                prevPosition++;

            var lastText = sql.Substring(prevPosition);
            dstSql += lastText;

            return dstSql;
        }

        private string GetNewName(List<ObjectRenamingResult> replacement, string oldName)
        {
            var newNames = replacement.Where(i => i.OldName.Equals(oldName, StringComparison.InvariantCultureIgnoreCase)).Select(i => i.NewName).Distinct().ToList();
            if (newNames.Count > 1) 
            {
                throw new AmbiguousMappingException("unknown", oldName);
            }

            if (!newNames.Any())
                return oldName.ToSnakeCase().RemoveDoubleUndescores();
            //throw new MappingNotFoundException("unknown", oldName);

            return newNames.First();
        }
    }
}
