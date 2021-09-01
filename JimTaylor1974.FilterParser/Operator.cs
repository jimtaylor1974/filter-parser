using System;
using System.Collections.Generic;
using System.Linq;

namespace JimTaylor1974.FilterParser
{
    public abstract class Operator : IToSqlAndFilter
    {
        public const string LhsOpRhs = "{lhs} {op} {rhs}";
        public const string FuncLhsRhs = "{op}({lhs},{rhs})";
        public const string FuncLhsRhsRhs1 = "{op}({lhs},{rhs},{rhs1})";
        public const string FuncLhs = "{op}({lhs})";
        public const string Func = "{op}()";

        private static readonly Dictionary<string, Operator> operators =
            typeof(Eq).Assembly.GetTypes()
                .Where(t => t.BaseType == typeof(Operator) && !t.IsAbstract)
                .Where(t => NotImplementedAttribute.For(t) == null)
                .Select(t =>
                {
                    var operatorAttribute = OperatorAttribute.For(t);
                    var op = (Operator)Activator.CreateInstance(t);

                    return new { operatorAttribute, op };
                })
                .ToDictionary(k => k.operatorAttribute.Filter, v => v.op, StringComparer.InvariantCultureIgnoreCase);

        private static readonly string[] functionNames = GetNamesByType(OperatorType.Function);

        private static string[] GetNamesByType(OperatorType operatorType)
        {
            return
                operators.Values.Where(op => op.operatorType.HasFlag(operatorType)).Select(op => op.filter).ToArray();
        }

        protected string filter = null;
        protected string sql = null;
        protected string filterTemplate = null;
        protected string sqlTemplate = null;
        protected string overloadFilterTemplate = null;
        protected string overloadSqlTemplate = null;
        protected OperatorType operatorType = OperatorType.Unknown;

        public Operator()
        {
            var operatorAttribute = OperatorAttribute.For(this.GetType());

            if (operatorAttribute != null)
            {
                filter = operatorAttribute.Filter;
                sql = operatorAttribute.Sql;
                filterTemplate = operatorAttribute.FilterTemplate;
                sqlTemplate = operatorAttribute.SqlTemplate;
                operatorType = operatorAttribute.OperatorType;
                overloadFilterTemplate = operatorAttribute.OverloadFilterTemplate;
                overloadSqlTemplate = operatorAttribute.OverloadSqlTemplate;
            }
        }

        public virtual string Sql => sql;

        public virtual string Filter => filter;

        public virtual IExpression ToExpression(ISqlFragment left, ISqlFragment right, ISqlFragment right1 = null)
        {
            return new OperatorExpression(this, left, right, right1);
        }

        public virtual IToSqlAndFilter GetTemplate(bool right1Supplied)
        {
            if (!string.IsNullOrWhiteSpace(overloadFilterTemplate) && !string.IsNullOrWhiteSpace(overloadSqlTemplate) && right1Supplied)
            {
                return new ToSqlAndFilter(overloadFilterTemplate, overloadSqlTemplate);
            }

            return new ToSqlAndFilter(filterTemplate, sqlTemplate);
        }

        public string ToString(Syntax syntax)
        {
            return syntax == Syntax.Filter
                ? Filter
                : Sql;
        }

        public override string ToString()
        {
            return ToString(Syntax.Sql);
        }

        public static Operator Parse(string operatorName)
        {
            if (operators.ContainsKey(operatorName))
            {
                return operators[operatorName];
            }

            return null;
        }

        public static string[] FunctionNames => functionNames;

        public virtual OperatorType OperatorType => operatorType;
    }

    [Operator(OperatorType.Whitespace, "", "\r\n")]
    public class NewLine : Operator
    {
    }

    [Operator(OperatorType.Grouping, "(", "(")]
    public class OpenGroup : Operator
    {
    }

    [Operator(OperatorType.Grouping, ")", ")")]
    public class CloseGroup : Operator
    {
    }

    [Operator(OperatorType.Binary, "or", "OR")]
    public class Or : Operator
    {
    }

    [Operator(OperatorType.Binary, "and", "AND")]
    public class And : Operator
    {
    }

    [Operator(OperatorType.Logical, "eq", "=", LhsOpRhs, LhsOpRhs)]
    public class Eq : Operator
    {
    }

    [Operator(OperatorType.Logical, "not", "NOT", "{op} {rhs}", "{op} {rhs}")]
    public class Not : Operator
    {
    }

    [Operator(OperatorType.Logical, "ex", "EXISTS", "{op} ({rhs})", "{op} ({rhs})")]
    public class Exists : Operator
    {
    }

    [Operator(OperatorType.Logical, "ge", ">=", LhsOpRhs, LhsOpRhs)]
    public class Ge : Operator
    {
    }

    [Operator(OperatorType.Logical, "gt", ">", LhsOpRhs, LhsOpRhs)]
    public class Gt : Operator
    {
    }

    [Operator(OperatorType.Logical, "le", "<=", LhsOpRhs, LhsOpRhs)]
    public class Le : Operator
    {
    }

    [Operator(OperatorType.Logical, "lt", "<", LhsOpRhs, LhsOpRhs)]
    public class Lt : Operator
    {
    }

    // Arithmetic Operators

    [Operator(OperatorType.Arithmetic, "add", "+", LhsOpRhs, LhsOpRhs)]
    public class Add : Operator
    {
    }

    [Operator(OperatorType.Arithmetic, "sub", "-", LhsOpRhs, LhsOpRhs)]
    public class Subtract : Operator
    {
    }

    [Operator(OperatorType.Arithmetic, "mul", "*", LhsOpRhs, LhsOpRhs)]
    public class Multiply : Operator
    {
    }

    [Operator(OperatorType.Arithmetic, "div", "/", LhsOpRhs, LhsOpRhs)]
    public class Divide : Operator
    {
    }

    [Operator(OperatorType.Arithmetic, "mod", "%", LhsOpRhs, LhsOpRhs)]
    public class Mod : Operator
    {
    }

    // http://docs.oasis-open.org/odata/odata/v4.0/errata02/os/complete/part2-url-conventions/odata-v4.0-errata02-os-part2-url-conventions-complete.html

    // Canonical functions

    [Operator(OperatorType.Function, "contains", "LIKE", FuncLhsRhs, "{lhs} {op} '%' + {rhs} + '%'")]
    public class Contains : Operator
    {
    }

    [Operator(OperatorType.Function, "endswith", "LIKE", FuncLhsRhs, "{lhs} {op} {rhs} + '%'")]
    public class EndsWith : Operator
    {
    }

    [Operator(OperatorType.Function, "startswith", "LIKE", FuncLhsRhs, "{lhs} {op} '%' + {rhs}")]
    public class StartsWith : Operator
    {
    }

    [Operator(OperatorType.Function, "length", "LEN", FuncLhs, FuncLhs)]
    public class Length : Operator
    {
    }

    [Operator(OperatorType.Function, "indexof", "CHARINDEX", FuncLhsRhs, FuncLhsRhs)]
    public class IndexOf : Operator
    {
    }

    [Operator(OperatorType.Function, "substring", "SUBSTRING", FuncLhsRhs, FuncLhsRhs, FuncLhsRhsRhs1, FuncLhsRhsRhs1)]
    public class Substring : Operator
    {
    }

    [NotImplemented("sql-case-sensitive-string-compare Select * from a_table where attribute = 'k' COLLATE Latin1_General_CS_AS ")]
    [Operator(OperatorType.Function, "tolower", "LOWER", FuncLhs, FuncLhs)]
    public class ToLower : Operator
    {
    }

    [NotImplemented("sql-case-sensitive-string-compare Select * from a_table where attribute = 'k' COLLATE Latin1_General_CS_AS")]
    [Operator(OperatorType.Function, "toupper", "UPPER", FuncLhs, FuncLhs)]
    public class ToUpper : Operator
    {
    }

    [Operator(OperatorType.Function, "trim", "LTRIM(RTRIM", FuncLhs, "{op}({lhs}))")]
    public class Trim : Operator
    {
    }

    [Operator(OperatorType.Function, "concat", "CONCAT", FuncLhsRhs, FuncLhsRhs)]
    public class Concat : Operator
    {
    }

    [Operator(OperatorType.Function, "year", "DATEPART", FuncLhs, "{op}(year, {lhs})")]
    public class Year : Operator
    {
    }

    [Operator(OperatorType.Function, "month", "DATEPART", FuncLhs, "{op}(month, {lhs})")]
    public class Month : Operator
    {
    }

    [Operator(OperatorType.Function, "day", "DATEPART", FuncLhs, "{op}(day, {lhs})")]
    public class Day : Operator
    {
    }

    [Operator(OperatorType.Function, "hour", "DATEPART", FuncLhs, "{op}(hour, {lhs})")]
    public class Hour : Operator
    {
    }

    [Operator(OperatorType.Function, "minute", "DATEPART", FuncLhs, "{op}(minute, {lhs})")]
    public class Minute : Operator
    {
    }

    [Operator(OperatorType.Function, "second", "DATEPART", FuncLhs, "{op}(second, {lhs})")]
    public class Second : Operator
    {
    }

    [Operator(OperatorType.Function, "fractionalseconds", "DATEPART", FuncLhs, "{op}(millisecond, {lhs})")]
    public class FractionalSeconds : Operator
    {
    }

    [NotImplemented("Date")]
    [Operator(OperatorType.Function, "date", "?", FuncLhs, "?")]
    public class Date : Operator
    {
    }

    [NotImplemented("Time")]
    [Operator(OperatorType.Function, "time", "?", FuncLhs, "?")]
    public class Time : Operator
    {
    }

    [NotImplemented("TotalOffsetMinutes")]
    [Operator(OperatorType.Function, "totaloffsetminutes", "?", FuncLhs, "?")]
    public class TotalOffsetMinutes : Operator
    {
    }

    [Operator(OperatorType.Function, "now", "GETUTCDATE", Func, Func)]
    public class Now : Operator
    {
    }

    [Operator(OperatorType.Function, "maxdatetime", "", Func, "CAST('9999-12-31 23:59:59.997' AS DATETIME)")]
    public class MaxDateTime : Operator
    {
    }

    [Operator(OperatorType.Function, "mindatetime", "", Func, "CAST('1753-01-01 00:00:00.000' AS DATETIME)")]
    public class MinDateTime : Operator
    {
    }

    [NotImplemented("TotalSeconds")]
    [Operator(OperatorType.Function, "totalseconds", "?", FuncLhs, "?")]
    public class TotalSeconds : Operator
    {
    }

    [Operator(OperatorType.Function, "round", "ROUND", FuncLhs, "{op}({lhs},0)")]
    public class Round : Operator
    {
    }

    [Operator(OperatorType.Function, "floor", "FLOOR", FuncLhs, FuncLhs)]
    public class Floor : Operator
    {
    }

    [Operator(OperatorType.Function, "ceiling", "CEILING", FuncLhs, FuncLhs)]
    public class Ceiling : Operator
    {
    }

    [NotImplemented("isof")]
    [Operator(OperatorType.Function, "isof", "?", "?", "?")]
    public class IsOf : Operator
    {
    }

    [NotImplemented("cast")]
    [Operator(OperatorType.Function, "cast", "?", "?", "?")]
    public class Cast : Operator
    {
    }

    [NotImplemented("geo.distance")]
    [Operator(OperatorType.Function, "geo.distance", "?", "?", "?")]
    public class GeoDistance : Operator
    {
    }

    [NotImplemented("geo.intersects")]
    [Operator(OperatorType.Function, "geo.intersects", "?", "?", "?")]
    public class GeoIntersects : Operator
    {
    }

    [NotImplemented("geo.length")]
    [Operator(OperatorType.Function, "geo.length", "?", "?", "?")]
    public class GeoLength : Operator
    {
    }

    [NotImplemented("any")]
    [Operator(OperatorType.Function, "any", "?", "?", "?")]
    public class Any : Operator
    {
    }

    [NotImplemented("all")]
    [Operator(OperatorType.Function, "all", "?", "?", "?")]
    public class All : Operator
    {
    }
}