using System;
using System.Linq;

namespace JimTaylor1974.FilterParser
{
    [AttributeUsage(AttributeTargets.Class)]
    public class OperatorAttribute : Attribute
    {
        private readonly string filter;
        private readonly string sql;
        private readonly string sqlTemplate;
        private readonly string filterTemplate;
        private readonly OperatorTypes operatorType;
        private readonly string overloadSqlTemplate;
        private readonly string overloadFilterTemplate;

        public OperatorAttribute(OperatorTypes operatorType, string filter, string sql)
        {
            this.operatorType = operatorType;
            this.sql = sql;
            this.filter = filter;

            this.sqlTemplate = sql;
            this.filterTemplate = filter;
        }

        public OperatorAttribute(OperatorTypes operatorType, string filter, string sql,
            string filterTemplate, string sqlTemplate)

            : this(operatorType, filter, sql)
        {
            this.sqlTemplate = sqlTemplate;
            this.filterTemplate = filterTemplate;
        }

        public OperatorAttribute(OperatorTypes operatorType, string filter, string sql,
            string filterTemplate, string sqlTemplate,
            string overloadFilterTemplate, string overloadSqlTemplate)

            : this(operatorType, filter, sql, filterTemplate, sqlTemplate)
        {
            this.overloadSqlTemplate = overloadSqlTemplate;
            this.overloadFilterTemplate = overloadFilterTemplate;
        }

        public string Filter { get { return filter; } }

        public string Sql { get { return sql; } }

        public string FilterTemplate { get { return filterTemplate; } }

        public string SqlTemplate { get { return sqlTemplate; } }

        public string OverloadFilterTemplate { get { return overloadFilterTemplate; } }

        public string OverloadSqlTemplate { get { return overloadSqlTemplate; } }

        public OperatorTypes OperatorType { get { return operatorType; } }

        public static OperatorAttribute For(Type type)
        {
            return type
                .GetCustomAttributes(typeof(OperatorAttribute), true)
                .Cast<OperatorAttribute>()
                .FirstOrDefault();
        }
    }
}