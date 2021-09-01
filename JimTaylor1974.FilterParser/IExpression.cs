using System.Collections.Generic;

namespace JimTaylor1974.FilterParser
{
    public interface IExpression : IToSqlAndFilter
    {
        object[] Args { get; }
        ICriteria ToCriteria();
        IEnumerable<IParameter> GetParameters();
    }
}