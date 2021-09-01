namespace JimTaylor1974.FilterParser
{
    public interface IParameter : IToSqlAndFilter
    {
        string Name { get; }
        object Value { get; }
    }
}