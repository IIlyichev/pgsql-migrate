using System.Data;
using System.Globalization;

namespace PgSqlMigrate.TypeMaps
{
    public abstract class TypeMapBase : ITypeMap
    {
        public Dictionary<DbType, SortedList<int, string>> Templates => _templates;

        private readonly Dictionary<DbType, SortedList<int, string>> _templates = new Dictionary<DbType, SortedList<int, string>>();
        private const string SizePlaceholder = "$size";
        protected const string PrecisionPlaceholder = "$precision";

        protected TypeMapBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            SetupTypeMaps();
        }

        protected abstract void SetupTypeMaps();

        protected void SetTypeMap(DbType type, string template)
        {
            EnsureHasList(type);
            _templates[type][-1] = template;
        }

        protected void SetTypeMap(DbType type, string template, int maxSize)
        {
            EnsureHasList(type);
            _templates[type][maxSize] = template;
        }

        /// <inheritdoc />
        public virtual string GetTypeMap(DbType type, int? size, int? precision)
        {
            if (!_templates.ContainsKey(type))
                throw new NotSupportedException($"Unsupported DbType '{type}'");

            var typeTemplates = _templates[type];

            if (size == null || typeTemplates.Count() == 1 && typeTemplates.First().Key == -1)
                return ReplacePlaceholders(_templates[type][-1], size: 0, precision);

            var sizeValue = size.Value;
            foreach (var entry in typeTemplates)
            {
                int capacity = entry.Key;
                string template = entry.Value;

                if (sizeValue <= capacity)
                    return ReplacePlaceholders(template, sizeValue, precision);
            }

            throw new NotSupportedException($"Unsupported DbType '{type}'");
        }

        /// <inheritdoc />
        [Obsolete]
        public virtual string GetTypeMap(DbType type, int size, int precision)
        {
            return GetTypeMap(type, (int?)size, precision);
        }

        private void EnsureHasList(DbType type)
        {
            if (!_templates.ContainsKey(type))
                _templates.Add(type, new SortedList<int, string>());
        }

        private string ReplacePlaceholders(string value, int? size, int? precision)
        {
            if (size != null)
            {
                value = value.Replace(SizePlaceholder, size.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (precision != null)
            {
                value = value.Replace(PrecisionPlaceholder, precision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return value;
        }

        public string GetCustomTypeMap(string definition, int? size, int? precision)
        {
            if (string.IsNullOrWhiteSpace(definition))
                throw new ArgumentNullException(nameof(definition));

            if (definition.Equals("geometry", StringComparison.InvariantCultureIgnoreCase)) 
            {
                return "citext";
            }

            throw new NotSupportedException($"Custom data type `{definition}` is not supported by `{this.GetType().FullName}`");
        }
    }

}
