﻿using System.Data;
using JetBrains.Annotations;

namespace PgSqlMigrate.TypeMaps
{
    /// <summary>
    /// A map of <see cref="DbType"/> to an SQL type
    /// </summary>
    public interface ITypeMap
    {
        /// <summary>
        /// Get the SQL type for a <see cref="DbType"/>
        /// </summary>
        /// <param name="type">The <see cref="DbType"/> to get the SQL type for</param>
        /// <param name="size">The requested size (in DB lingua: precision)</param>
        /// <param name="precision">The requested precision (in DB lingua: scale)</param>
        /// <returns>The SQL type</returns>
        [NotNull]
        [Obsolete]
        string GetTypeMap(DbType type, int size, int precision);

        /// <summary>
        /// Get the SQL type for a <see cref="DbType"/>
        /// </summary>
        /// <param name="type">The <see cref="DbType"/> to get the SQL type for</param>
        /// <param name="size">The requested size (in DB lingua: precision)</param>
        /// <param name="precision">The requested precision (in DB lingua: scale)</param>
        /// <returns>The SQL type</returns>
        [NotNull]
        string GetTypeMap(DbType type, int? size, int? precision);

        [NotNull]
        string GetCustomTypeMap(string definition, int? size, int? precision);

        /// <summary>
        /// Provides visibility of templates. Is used for reverse engineering
        /// </summary>
        Dictionary<DbType, SortedList<int, string>> Templates { get; }
    }
}
