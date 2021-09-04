using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
                .Select(t =>
                {
                    var operatorAttribute = OperatorAttribute.For(t);
                    var op = (Operator)Activator.CreateInstance(t);

                    return new { operatorAttribute, op };
                })
                .ToDictionary(k => k.operatorAttribute.Filter, v => v.op, StringComparer.InvariantCultureIgnoreCase);

        private static readonly string[] functionNames = GetNamesByType(OperatorTypes.Function);

        private static string[] GetNamesByType(OperatorTypes operatorType)
        {
            return
                operators.Values.Where(op => op.Implemented && op.operatorType.HasFlag(operatorType)).Select(op => op.filter).ToArray();
        }

        protected bool implemented;
        protected string filter;
        protected string sql;
        protected string filterTemplate;
        protected string sqlTemplate;
        protected string overloadFilterTemplate;
        protected string overloadSqlTemplate;
        protected OperatorTypes operatorType = OperatorTypes.None;

        protected Operator()
        {
            var type = this.GetType();
            var operatorAttribute = OperatorAttribute.For(type);

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

            implemented = NotImplementedAttribute.For(type) == null;
        }

        public virtual bool Implemented => implemented;

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
                var @operator = operators[operatorName];

                if (@operator.Implemented)
                {
                    return @operator;
                }
            }

            return null;
        }

        public static string[] FunctionNames => functionNames;

        public virtual OperatorTypes OperatorType => operatorType;

        private static (string[] headers, string[][] rows) Documentation(bool implementedOnly)
        {
            var operatorData = new List<string[]>();

            string GetText(Operator @operator)
            {
                if (@operator.Filter.Any(char.IsLetter))
                {
                    return @operator.Filter;
                }

                var nameAsText = Regex.Replace(@operator.GetType().Name,
                    @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");

                return $"{nameAsText}" + (string.IsNullOrWhiteSpace(@operator.Filter) ? @operator.Filter : $": {@operator.Filter}");
            }

            foreach (var @operator in operators.Values.Where(o => o.Implemented || !implementedOnly).OrderBy(o => o.OperatorType).ThenBy(o => o.Filter))
            {
                var row = new List<string>
                {
                    @operator.OperatorType.ToString(),
                    GetText(@operator)
                };

                if (!implementedOnly)
                {
                    row.Add(@operator.Implemented ? "" : "**Not implemented**");
                }

                operatorData.Add(row.ToArray());
            }

            var headers = new List<string>
            {
                "Type",
                "Operator"
            };

            if (!implementedOnly)
            {
                headers.Add("");
            }

            return (headers.ToArray(), operatorData.ToArray());
        }

        public static string DocumentAsMarkdown(bool implementedOnly = true)
        {
            var (headers, rows) = Documentation(implementedOnly);

            var sb = new StringBuilder();

            sb.Append(@"| ");
            sb.Append(string.Join(" | ", headers));
            sb.AppendLine(@" |");

            sb.Append(@"| ");
            sb.Append(string.Join(" | ", headers.Select(h => " --- ")));
            sb.AppendLine(@" |");

            foreach (var row in rows)
            {
                sb.Append("| ");
                sb.Append(string.Join(" | ", row));
                sb.AppendLine(" |");
            }

            return sb.ToString();
        }

        public static string DocumentAsHtml(bool implementedOnly = true)
        {
            var (headers, rows) = Documentation(implementedOnly);

            var sb = new StringBuilder();

            sb.AppendLine(@"<table>");
            sb.AppendLine(@"<tbody>");
            sb.AppendLine(@"<tr>");
            sb.AppendLine(string.Join("", headers.Select(h => $"<th>{h}</th>")));
            sb.AppendLine(@"</tr>");
            sb.AppendLine(@"</tbody>");
            sb.AppendLine(@"<tbody>");
            foreach (var row in rows)
            {
                sb.AppendLine("<tr>");
                foreach (var cellValue in row)
                {
                    sb.AppendLine($"<td>{cellValue}</td>");
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine(@"</tbody>");
            sb.AppendLine(@"</table>");

            return sb.ToString();
        }
    }

    [Operator(OperatorTypes.Whitespace, "", "\r\n")]
    public class NewLine : Operator
    {
    }

    [Operator(OperatorTypes.Grouping, "(", "(")]
    public class OpenGroup : Operator
    {
    }

    [Operator(OperatorTypes.Grouping, ")", ")")]
    public class CloseGroup : Operator
    {
    }

    [Operator(OperatorTypes.Binary, "or", "OR")]
    public class Or : Operator
    {
    }

    [Operator(OperatorTypes.Binary, "and", "AND")]
    public class And : Operator
    {
    }

    [Operator(OperatorTypes.Logical, "eq", "=", LhsOpRhs, LhsOpRhs)]
    public class Eq : Operator
    {
    }

    [Operator(OperatorTypes.Logical, "not", "NOT", "{op} {rhs}", "{op} {rhs}")]
    public class Not : Operator
    {
    }

    [Operator(OperatorTypes.Logical, "ex", "EXISTS", "{op} ({rhs})", "{op} ({rhs})")]
    public class Exists : Operator
    {
    }

    [Operator(OperatorTypes.Logical, "ge", ">=", LhsOpRhs, LhsOpRhs)]
    public class Ge : Operator
    {
    }

    [Operator(OperatorTypes.Logical, "gt", ">", LhsOpRhs, LhsOpRhs)]
    public class Gt : Operator
    {
    }

    [Operator(OperatorTypes.Logical, "le", "<=", LhsOpRhs, LhsOpRhs)]
    public class Le : Operator
    {
    }

    [Operator(OperatorTypes.Logical, "lt", "<", LhsOpRhs, LhsOpRhs)]
    public class Lt : Operator
    {
    }

    // Arithmetic Operators

    [Operator(OperatorTypes.Arithmetic, "add", "+", LhsOpRhs, LhsOpRhs)]
    public class Add : Operator
    {
    }

    [Operator(OperatorTypes.Arithmetic, "sub", "-", LhsOpRhs, LhsOpRhs)]
    public class Subtract : Operator
    {
    }

    [Operator(OperatorTypes.Arithmetic, "mul", "*", LhsOpRhs, LhsOpRhs)]
    public class Multiply : Operator
    {
    }

    [Operator(OperatorTypes.Arithmetic, "div", "/", LhsOpRhs, LhsOpRhs)]
    public class Divide : Operator
    {
    }

    [Operator(OperatorTypes.Arithmetic, "mod", "%", LhsOpRhs, LhsOpRhs)]
    public class Mod : Operator
    {
    }

    // http://docs.oasis-open.org/odata/odata/v4.0/errata02/os/complete/part2-url-conventions/odata-v4.0-errata02-os-part2-url-conventions-complete.html

    // Canonical functions

    [Operator(OperatorTypes.Function, "contains", "LIKE", FuncLhsRhs, "{lhs} {op} '%' + {rhs} + '%'")]
    public class Contains : Operator
    {
    }

    [Operator(OperatorTypes.Function, "endswith", "LIKE", FuncLhsRhs, "{lhs} {op} {rhs} + '%'")]
    public class EndsWith : Operator
    {
    }

    [Operator(OperatorTypes.Function, "startswith", "LIKE", FuncLhsRhs, "{lhs} {op} '%' + {rhs}")]
    public class StartsWith : Operator
    {
    }

    [Operator(OperatorTypes.Function, "length", "LEN", FuncLhs, FuncLhs)]
    public class Length : Operator
    {
    }

    [Operator(OperatorTypes.Function, "indexof", "CHARINDEX", FuncLhsRhs, FuncLhsRhs)]
    public class IndexOf : Operator
    {
    }

    [Operator(OperatorTypes.Function, "substring", "SUBSTRING", FuncLhsRhs, FuncLhsRhs, FuncLhsRhsRhs1, FuncLhsRhsRhs1)]
    public class Substring : Operator
    {
    }

    [NotImplemented("sql-case-sensitive-string-compare Select * from a_table where attribute = 'k' COLLATE Latin1_General_CS_AS ")]
    [Operator(OperatorTypes.Function, "tolower", "LOWER", FuncLhs, FuncLhs)]
    public class ToLower : Operator
    {
    }

    [NotImplemented("sql-case-sensitive-string-compare Select * from a_table where attribute = 'k' COLLATE Latin1_General_CS_AS")]
    [Operator(OperatorTypes.Function, "toupper", "UPPER", FuncLhs, FuncLhs)]
    public class ToUpper : Operator
    {
    }

    [Operator(OperatorTypes.Function, "trim", "LTRIM(RTRIM", FuncLhs, "{op}({lhs}))")]
    public class Trim : Operator
    {
    }

    [Operator(OperatorTypes.Function, "concat", "CONCAT", FuncLhsRhs, FuncLhsRhs)]
    public class Concat : Operator
    {
    }

    [Operator(OperatorTypes.Function, "year", "DATEPART", FuncLhs, "{op}(year, {lhs})")]
    public class Year : Operator
    {
    }

    [Operator(OperatorTypes.Function, "month", "DATEPART", FuncLhs, "{op}(month, {lhs})")]
    public class Month : Operator
    {
    }

    [Operator(OperatorTypes.Function, "day", "DATEPART", FuncLhs, "{op}(day, {lhs})")]
    public class Day : Operator
    {
    }

    [Operator(OperatorTypes.Function, "hour", "DATEPART", FuncLhs, "{op}(hour, {lhs})")]
    public class Hour : Operator
    {
    }

    [Operator(OperatorTypes.Function, "minute", "DATEPART", FuncLhs, "{op}(minute, {lhs})")]
    public class Minute : Operator
    {
    }

    [Operator(OperatorTypes.Function, "second", "DATEPART", FuncLhs, "{op}(second, {lhs})")]
    public class Second : Operator
    {
    }

    [Operator(OperatorTypes.Function, "fractionalseconds", "DATEPART", FuncLhs, "{op}(millisecond, {lhs})")]
    public class FractionalSeconds : Operator
    {
    }

    [NotImplemented("Date")]
    [Operator(OperatorTypes.Function, "date", "?", FuncLhs, "?")]
    public class Date : Operator
    {
    }

    [NotImplemented("Time")]
    [Operator(OperatorTypes.Function, "time", "?", FuncLhs, "?")]
    public class Time : Operator
    {
    }

    [NotImplemented("TotalOffsetMinutes")]
    [Operator(OperatorTypes.Function, "totaloffsetminutes", "?", FuncLhs, "?")]
    public class TotalOffsetMinutes : Operator
    {
    }

    [Operator(OperatorTypes.Function, "now", "GETUTCDATE", Func, Func)]
    public class Now : Operator
    {
    }

    [Operator(OperatorTypes.Function, "maxdatetime", "", Func, "CAST('9999-12-31 23:59:59.997' AS DATETIME)")]
    public class MaxDateTime : Operator
    {
    }

    [Operator(OperatorTypes.Function, "mindatetime", "", Func, "CAST('1753-01-01 00:00:00.000' AS DATETIME)")]
    public class MinDateTime : Operator
    {
    }

    [NotImplemented("TotalSeconds")]
    [Operator(OperatorTypes.Function, "totalseconds", "?", FuncLhs, "?")]
    public class TotalSeconds : Operator
    {
    }

    [Operator(OperatorTypes.Function, "round", "ROUND", FuncLhs, "{op}({lhs},0)")]
    public class Round : Operator
    {
    }

    [Operator(OperatorTypes.Function, "floor", "FLOOR", FuncLhs, FuncLhs)]
    public class Floor : Operator
    {
    }

    [Operator(OperatorTypes.Function, "ceiling", "CEILING", FuncLhs, FuncLhs)]
    public class Ceiling : Operator
    {
    }

    [NotImplemented("isof")]
    [Operator(OperatorTypes.Function, "isof", "?", "?", "?")]
    public class IsOf : Operator
    {
    }

    [NotImplemented("cast")]
    [Operator(OperatorTypes.Function, "cast", "?", "?", "?")]
    public class Cast : Operator
    {
    }

    [NotImplemented("geo.distance")]
    [Operator(OperatorTypes.Function, "geo.distance", "?", "?", "?")]
    public class GeoDistance : Operator
    {
    }

    [NotImplemented("geo.intersects")]
    [Operator(OperatorTypes.Function, "geo.intersects", "?", "?", "?")]
    public class GeoIntersects : Operator
    {
    }

    [NotImplemented("geo.length")]
    [Operator(OperatorTypes.Function, "geo.length", "?", "?", "?")]
    public class GeoLength : Operator
    {
    }

    [NotImplemented("any")]
    [Operator(OperatorTypes.Function, "any", "?", "?", "?")]
    public class Any : Operator
    {
    }

    [NotImplemented("all")]
    [Operator(OperatorTypes.Function, "all", "?", "?", "?")]
    public class All : Operator
    {
    }
}