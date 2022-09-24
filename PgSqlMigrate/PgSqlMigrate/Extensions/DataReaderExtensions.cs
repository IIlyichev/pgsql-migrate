using System.Data;

namespace PgSqlMigrate.Extensions
{
    /// <summary>
    /// DtaaReader extensions
    /// </summary>
    public static class DataReaderExtensions
    {
        /// <summary>
        /// Get nullable int value
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static int? GetNullableInt(this IDataReader dataReader, int idx) 
        { 
            return dataReader.IsDBNull(idx)
                ? (int?)null
                : dataReader.GetInt32(idx);
        }

        /// <summary>
        /// Get nullable byte value
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static byte? GetNullableByte(this IDataReader dataReader, int idx)
        {
            return dataReader.IsDBNull(idx)
                ? (byte?)null
                : dataReader.GetByte(idx);
        }
    }
}
