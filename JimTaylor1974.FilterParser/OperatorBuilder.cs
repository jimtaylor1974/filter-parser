using System.Collections.Generic;
using System.Linq;

namespace JimTaylor1974.FilterParser
{
    internal class OperatorBuilder
    {
        private readonly Counter counter;
        private readonly ResolveField resolveField;
        private readonly List<ISqlFragment> sqlFragments = new List<ISqlFragment>();

        public OperatorBuilder(Counter counter, ResolveField resolveField)
        {
            this.counter = counter;
            this.resolveField = resolveField;
        }

        private ISqlFragment Parse(
            ISqlFragment sqlFragment,
            Operator @operator,
            ArgumentPosition argumentPosition)
        {
            var unparsedTokenFragment = sqlFragment as UnparsedTokenFragment;

            if (unparsedTokenFragment == null)
            {
                // already a known type e.g. Operator
                return sqlFragment;
            }

            var token = unparsedTokenFragment.Token;

            var value = token.Value;

            throw new CriteriaParseException($"Unable to parse \'{value}\' as field, constant or value. Operator = {@operator.Filter}, argument position = {argumentPosition}.");
        }

        public void Add(ISqlFragment sqlFragment)
        {
            sqlFragments.Add(sqlFragment);
        }

        public IExpression ToExpression()
        {
            Operator @operator = sqlFragments.OfType<Operator>().FirstOrDefault();

            if (@operator == null)
            {
                throw new CriteriaParseException("Missing operation");
            }

            var left = new List<ISqlFragment>();
            var right = new List<ISqlFragment>();
            var right1 = new List<ISqlFragment>();
            int commaCount = 0;

            bool leftOfOperator = @operator.OperatorType != OperatorType.Function;
            for (var index = 0; index < sqlFragments.Count; index++)
            {
                var sqlFragment = sqlFragments[index];

                if (sqlFragment == @operator)
                {
                    leftOfOperator = false;
                    continue;
                }

                ISqlFragment parsedSqlFragment;

                if (leftOfOperator)
                {
                    parsedSqlFragment = Parse(sqlFragment, @operator, ArgumentPosition.Left);
                    left.Add(parsedSqlFragment);
                }
                else
                {
                    if (sqlFragment is Comma)
                    {
                        commaCount++;
                        continue;
                    }

                    if (sqlFragment is Operator)
                    {
                        // this is the right hand side of the function

                        var functionExpression = OperatorExpression(left, right, right1, @operator);

                        var remaining = sqlFragments.Skip(index).ToArray();

                        var builder = new OperatorBuilder(counter, resolveField);
                        builder.Add(functionExpression);
                        foreach (var fragment in remaining)
                        {
                            builder.Add(fragment);
                        }

                        var expression = builder.ToExpression();

                        return expression;
                    }

                    if (@operator.OperatorType == OperatorType.Function)
                    {
                        switch (commaCount)
                        {
                            case 0:
                                parsedSqlFragment = Parse(sqlFragment, @operator, ArgumentPosition.Left);
                                left.Add(parsedSqlFragment);
                                break;
                            case 1:
                                parsedSqlFragment = Parse(sqlFragment, @operator, ArgumentPosition.Right);
                                right.Add(parsedSqlFragment);
                                break;
                            default:
                                parsedSqlFragment = Parse(sqlFragment, @operator, ArgumentPosition.Right1);
                                right1.Add(parsedSqlFragment);
                                break;
                        }
                    }
                    else
                    {
                        parsedSqlFragment = Parse(sqlFragment, @operator, ArgumentPosition.Right);
                        right.Add(parsedSqlFragment);
                    }
                }
            }

            return OperatorExpression(left, right, right1, @operator);
        }

        private static IExpression OperatorExpression(List<ISqlFragment> left, List<ISqlFragment> right, List<ISqlFragment> right1, Operator @operator)
        {
            ISqlFragment leftExpression = left.Any() ? new SqlFragmentExpression(left) : null;
            ISqlFragment rightExpression = right.Any() ? new SqlFragmentExpression(right) : null;
            ISqlFragment right1Expression = right1.Any() ? new SqlFragmentExpression(right1) : null;

            return @operator.ToExpression(leftExpression, rightExpression, right1Expression);
        }
    }
}