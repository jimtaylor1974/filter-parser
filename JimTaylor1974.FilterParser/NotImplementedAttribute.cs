using System;
using System.Linq;

namespace JimTaylor1974.FilterParser
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NotImplementedAttribute : Attribute
    {
        public NotImplementedAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; set; }

        public static NotImplementedAttribute For(Type type)
        {
            return type
                .GetCustomAttributes(typeof(NotImplementedAttribute), true)
                .Cast<NotImplementedAttribute>()
                .FirstOrDefault();
        }
    }
}