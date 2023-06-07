using System.Data;
using System.Text.RegularExpressions;

namespace PgSqlMigrate.TypeMaps
{
    /// <summary>
    /// Type map helper
    /// </summary>
    public class TypeMapHelper
    {
        /// <summary>
        /// Return dictionary of data types.
        /// </summary>
        /// <param name="typeMap">Map that is used by the FluentMigrator for sql statements generation</param>
        /// <param name="conflictResolver">Conflic resolver. Is executed when single type name is mapped to multiple DbTypes</param>
        /// <returns></returns>
        public static Dictionary<string, DbType> GetReversedTypesMap(ITypeMap typeMap, Func<string, List<DbType>, DbType> conflictResolver)
        {
            var withParamsRegex = new Regex(@"(?'datatype'[^\(]+)\((?'params'[^\)]+)\)");

            var dts = new List<DataTypeNameWithTemplates>();

            var dbTypes = typeMap.Templates.Keys;
            foreach (var dbType in dbTypes)
            {
                var templates = typeMap.Templates[dbType];
                foreach (var template in templates)
                {
                    var dataTypeName = withParamsRegex.IsMatch(template.Value)
                        ? withParamsRegex.Match(template.Value).Groups["datatype"].Value
                        : template.Value;
                    dataTypeName = dataTypeName.Trim().ToLower();

                    var dt = dts.FirstOrDefault(dt => dt.Name == dataTypeName);
                    if (dt == null)
                    {
                        dt = new DataTypeNameWithTemplates(dataTypeName);
                        dts.Add(dt);
                    }
                    dt.Templates.Add(new KeyValuePair<DbType, string>(dbType, template.Value));
                }
            }

            var result = new Dictionary<string, DbType>();

            foreach (var dt in dts) 
            {
                var mappedDbTypes = dt.Templates.Select(t => t.Key).Distinct().ToList();

                var dbType = mappedDbTypes.Count() == 1
                    ? mappedDbTypes.First()
                    : conflictResolver.Invoke(dt.Name, mappedDbTypes);
                
                result.Add(dt.Name, dbType);
            }

            return result;
        }

        internal class DataTypeNameWithTemplates 
        { 
            public string Name { get; set; }
            public List<KeyValuePair<DbType, string>> Templates { get; set; }

            public DataTypeNameWithTemplates(string name)
            {
                Name = name;
                Templates = new List<KeyValuePair<DbType, string>>();
            }
        }
    }
}
