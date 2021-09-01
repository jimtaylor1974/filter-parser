using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JimTaylor1974.FilterParser
{
    public partial class Criteria : ICriteria
    {
        private readonly CriteriaType criteriaType;
        private readonly List<ICriteria> children = new List<ICriteria>();

        private Criteria(CriteriaType criteriaType)
        {
            this.criteriaType = criteriaType;
        }

        public CriteriaType CriteriaType
        {
            get { return criteriaType; }
        }

        public static ICriteria FromExpression(IExpression expression)
        {
            return new Criteria(CriteriaType.Expression)
            {
                Expression = expression
            };
        }

        public static ICriteria And(params ICriteria[] andCriteria)
        {
            var criteria = new Criteria(CriteriaType.And);

            foreach (var c in andCriteria)
            {
                criteria.children.Add(c);
            }

            return criteria;
        }

        public static ICriteria Or(params ICriteria[] orCriteria)
        {
            var criteria = new Criteria(CriteriaType.Or);

            foreach (var c in orCriteria)
            {
                criteria.children.Add(c);
            }

            return criteria;
        }

        public static ICriteria And(params IExpression[] expressions)
        {
            var criteria = new Criteria(CriteriaType.And);

            foreach (var expression in expressions)
            {
                criteria.children.Add(FromExpression(expression));
            }

            return criteria;
        }

        public static ICriteria Or(params IExpression[] expressions)
        {
            var criteria = new Criteria(CriteriaType.Or);

            foreach (var expression in expressions)
            {
                criteria.children.Add(FromExpression(expression));
            }

            return criteria;
        }

        public ICriteria Add(ICriteria criteria)
        {
            children.Add(criteria);

            return this;
        }

        public IExpression Expression { get; protected set; }

        public IParameter[] GetDistinctParameters()
        {
            var parameters = new Dictionary<string, IParameter>();

            foreach (var parameter in GetAllParameters())
            {
                if (!parameters.ContainsKey(parameter.Name))
                {
                    parameters.Add(parameter.Name, parameter);
                }
            }

            return parameters.Values.ToArray();
        }
        
        public IEnumerable<IParameter> GetAllParameters()
        {
            if (Expression != null)
            {
                var parameters = Expression.GetParameters();

                foreach (var parameter in parameters)
                {
                    yield return parameter;
                }
            }

            foreach (var criteria in children)
            {
                foreach (var parameter in criteria.GetAllParameters())
                {
                    yield return parameter;
                }
            }
        }

        public bool IsEmpty
        {
            get { return children.Count == 0 && Expression == null; }
        }

        public string ToString(Syntax syntax)
        {
            var builder = new StringBuilder();

            if (Expression == null)
            {
                var separator = CriteriaType == CriteriaType.And
                    ? new Separator(new NewLine().ToString(syntax) + " " + new And().ToString(syntax) + " ")
                    : new Separator(new NewLine().ToString(syntax) + " " + new Or().ToString(syntax) + " ");

                builder.Append("(");
                foreach (var c in children)
                {
                    builder.Append(separator.Value());
                    builder.Append(c.ToString(syntax));
                }

                builder.Append(")");
            }
            else
            {
                builder.Append(Expression.ToString(syntax));
            }

            return builder.ToString();
        }

        public override string ToString()
        {
            return ToString(Syntax.Sql);
        }
    }
}