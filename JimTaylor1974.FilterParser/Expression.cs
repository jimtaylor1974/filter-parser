using System;
using System.Collections.Generic;
using System.Linq;

namespace JimTaylor1974.FilterParser
{
    public abstract class ExpressionBase : IExpression
    {
        protected readonly object[] args;

        protected ExpressionBase(object[] args)
        {
            this.args = args;
        }

        public abstract string ToString(Syntax syntax);

        public object[] Args
        {
            get { return args; }
        }

        public virtual ICriteria ToCriteria()
        {
            return Criteria.FromExpression(this);
        }

        public virtual IEnumerable<IParameter> GetParameters()
        {
            return args.OfType<IExpression>().SelectMany(e => e.GetParameters()).Union(args.OfType<IParameter>());
        }

        public override string ToString()
        {
            return ToString(Syntax.Sql);
        }

        protected object Convert(object toConvert, Syntax syntax, Func<Syntax, object, string> toString)
        {
            if (toConvert is IToSqlAndFilter)
            {
                return toString(syntax, toConvert);
            }

            return toConvert;
        }
    }

    public class OperatorExpression : ExpressionBase
    {
        private readonly Operator @operator;
        private readonly ISqlFragment lhs;
        private readonly ISqlFragment rhs;
        private readonly ISqlFragment rhs1;

        public OperatorExpression(Operator @operator, ISqlFragment lhs, ISqlFragment rhs, ISqlFragment rhs1 = null)
            : base(new[] { (object)lhs, (object)rhs, (object)rhs1 })
        {
            this.@operator = @operator;
            this.lhs = lhs;
            this.rhs = rhs;
            this.rhs1 = rhs1;
        }

        public override string ToString(Syntax syntax)
        {
            var replacements = new
            {
                op = @operator.ToString(syntax),
                lhs = (string)Convert(lhs, syntax, (s, o) => ((IToSqlAndFilter)o).ToString(s)),
                rhs = (string)Convert(rhs, syntax, (s, o) => ((IToSqlAndFilter)o).ToString(s)),
                rhs1 = (string)Convert(rhs1, syntax, (s, o) => ((IToSqlAndFilter)o).ToString(s))
            };

            var format = @operator.GetTemplate(rhs1 != null).ToString(syntax)
                .Replace("{op}", replacements.op)
                .Replace("{lhs}", replacements.lhs)
                .Replace("{rhs}", replacements.rhs)
                .Replace("{rhs1}", replacements.rhs1);

            return format;
        }
    }

    public class GroupExpression : IExpression
    {
        private readonly IExpression expression;

        public GroupExpression(IExpression expression)
        {
            this.expression = expression;
        }

        public string ToString(Syntax syntax)
        {
            return $"({expression.ToString(syntax)})";
        }

        public override string ToString()
        {
            return ToString(Syntax.Sql);
        }

        public object[] Args
        {
            get { return new[] { (object)expression }; }
        }

        public ICriteria ToCriteria()
        {
            return expression.ToCriteria();
        }

        public IEnumerable<IParameter> GetParameters()
        {
            return expression.GetParameters();
        }
    }

    public class Expression : ExpressionBase
    {
        private readonly string format;

        public Expression(string format, params object[] args)
            : base(args)
        {
            this.format = format;
        }

        public override string ToString(Syntax syntax)
        {
            return string.Format(format, args.Select(arg => Convert(arg, syntax, (s, o) => ((IToSqlAndFilter)o).ToString(s))).ToArray());
        }
    }

    public class SqlFragmentExpression : ExpressionBase
    {
        public SqlFragmentExpression(params object[] args)
            : base(args)
        {
        }

        public SqlFragmentExpression(IEnumerable<ISqlFragment> args)
            : base(args.Cast<object>().ToArray())
        {
        }

        public override string ToString(Syntax syntax)
        {
            var converted = args
                .Select(arg => Convert(arg, syntax, (s, o) => ((IToSqlAndFilter) o).ToString(syntax)))
                .ToArray();

            return string.Join(string.Empty, converted);
        }
    }
}