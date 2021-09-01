using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace JimTaylor1974.FilterParser
{
    public static class Extensions
    {
        public static string EscapeSingleQuotes(this string s)
        {
            return s == null ? null : s.Replace("'", "''");
        }

        public static string SurroundWithSingleQuotes(this string s)
        {
            return "'" + s + "'";
        }

        public static string SurroundWithSquareBrackets(this string s)
        {
            if (s == "*")
            {
                return s;
            }

            return string.Join('.',
                s.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(p => "[" + p.Trim('[', ']') + "]"));
        }

        public static string ToSqlBit(this bool b)
        {
            return Math.Abs(Convert.ToInt32(b)).ToString(CultureInfo.InvariantCulture);
        }

        public static string ToSqlType(this DbType dbType)
        {
            switch (dbType)
            {
                case DbType.Int32:
                    return "INT";
                case DbType.AnsiString:
                case DbType.Binary:
                case DbType.Byte:
                case DbType.Boolean:
                case DbType.Currency:
                case DbType.Date:
                case DbType.DateTime:
                case DbType.Decimal:
                case DbType.Double:
                case DbType.Guid:
                case DbType.Int16:
                case DbType.Int64:
                case DbType.Object:
                case DbType.SByte:
                case DbType.Single:
                case DbType.String:
                case DbType.Time:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.VarNumeric:
                case DbType.AnsiStringFixedLength:
                case DbType.StringFixedLength:
                case DbType.Xml:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException("Unsupported DbType " + dbType);
            }
        }

        public static string ToSqlLiteral(this object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return "NULL";
            }

            var type = value.GetType();

            if (type == typeof(string))
            {
                return ((string)value).EscapeSingleQuotes().SurroundWithSingleQuotes();
            }

            if (type == typeof(bool))
            {
                return ((bool)value).ToSqlBit();
            }

            if (type == typeof(DateTime))
            {
                return ((DateTime)value).ToString("YYYY-MM-dd HH:mm:ss.ttt").SurroundWithSingleQuotes();
            }

            if (type == typeof(Guid))
            {
                return ((Guid)value).ToString("D").SurroundWithSingleQuotes();
            }

            return value.ToString();
        }
    }
}