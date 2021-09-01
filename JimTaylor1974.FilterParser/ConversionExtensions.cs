using System;
using System.ComponentModel;
using System.Data.SqlTypes;
using Newtonsoft.Json.Linq;

namespace JimTaylor1974.FilterParser
{
    internal static class ConversionExtensions
    {
        // http://extensionmethod.net/csharp/string/parse-t

        public static T Convert<T>(this object value)
        {
            var result = (T)Convert(value, typeof(T));

            return result;
        }

        public static object Convert(this object value, Type type)
        {
            var converter = GetTypeConverter(type, value);
            var result = Convert(converter, type, value);

            return result;
        }

        public static T ConvertSafe<T>(this object value)
        {
            T result;
            TryConvert(value, out result);
            return result;
        }

        public static object ConvertSafe(this object value, Type type)
        {
            object result;
            TryConvert(value, type, out result);
            return result;
        }

        public static bool TryConvert<T>(this object value, out T result)
        {
            result = default(T);

            var converter = TypeDescriptor.GetConverter(typeof(T));

            if (converter.IsValid(value))
            {
                result = (T)Convert(converter, typeof(T), value);
                return true;
            }

            return false;
        }

        public static bool TryConvert(this object value, Type type, out object result)
        {
            result = GetDefaultValue(type);

            var converter = GetTypeConverter(type, value);

            if (converter.IsValid(value))
            {
                result = Convert(converter, type, value);
                return true;
            }

            return false;
        }

        private static TypeConverter GetTypeConverter(Type type, object value)
        {
            if (value is JValue)
            {
                return new JValueTypeConverter(type);
            }

            return TypeDescriptor.GetConverter(type);
        }

        private static object Convert(TypeConverter converter, Type type, object value)
        {
            if (value is int && converter.GetType() == typeof(EnumConverter))
            {
                return Enum.Parse(type, value.ToString());
            }

            return converter.ConvertFrom(value);
        }

        private static object GetDefaultValue(this Type t)
        {
            if (t.IsValueType)
            {
                if (t == typeof(DateTime))
                {
                    return SqlDateTime.MinValue.Value;
                }

                return Activator.CreateInstance(t);
            }

            return null;
        }
    }
}