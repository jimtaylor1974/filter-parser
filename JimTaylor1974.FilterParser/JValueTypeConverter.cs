using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace JimTaylor1974.FilterParser
{
    public class JValueTypeConverter : TypeConverter
    {
        private readonly Type destinationType;

        public JValueTypeConverter(Type destinationType)
        {
            this.destinationType = destinationType;
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            try
            {
                ConvertFrom(context, CultureInfo.InvariantCulture, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            value = GetValue(value);

            if (destinationType.IsNullable() && value is JValue && ((JValue)value).Type == JTokenType.String && ((JValue)value).Value == string.Empty)
            {
                return null;
            }

            return Convert.ChangeType(value, destinationType);
        }

        private object GetValue(object value)
        {
            return value;
        }
    }
}