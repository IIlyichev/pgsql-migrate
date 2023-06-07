using System.Text.RegularExpressions;

namespace PgSqlMigrate.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Convert string to snake_case
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToSnakeCaseOld(this string input)
        {
            if (string.IsNullOrEmpty(input)) { return input; }

            var startUnderscores = Regex.Match(input, @"^_+");
            return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
        }

        public static string ToSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) { return input; }

            return Regex.Replace(Regex.Replace(input, "(.)([A-Z][a-z]+)", "$1_$2"), "([a-z0-9])([A-Z])", "$1_$2").ToLower().RemoveDoubleUndescores();
        }        

        /// <summary>
        /// Remove multiple underacore `_` characters from string
        /// </summary>
        /// <returns></returns>
        public static string RemoveDoubleUndescores(this string input) 
        {
            return Regex.Replace(input, @"[_]{2,}", "_");
        }

        /// <summary>
        /// Wrap specified <paramref name="value"/> with double quotes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Quote(this string value) 
        {
            return !string.IsNullOrWhiteSpace(value) 
                ? "\"" + value + "\"" 
                : value;
        }
    }
}
