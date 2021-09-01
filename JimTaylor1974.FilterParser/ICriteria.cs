using System.Collections.Generic;

namespace JimTaylor1974.FilterParser
{
    public interface ICriteria : IToSqlAndFilter
    {
        ICriteria Add(ICriteria criteria);

        CriteriaType CriteriaType { get; }

        IExpression Expression { get; }

        IEnumerable<IParameter> GetAllParameters();

        IParameter[] GetDistinctParameters();

        bool IsEmpty { get; }
    }
}