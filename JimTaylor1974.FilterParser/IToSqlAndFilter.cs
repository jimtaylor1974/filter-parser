namespace JimTaylor1974.FilterParser
{
    public interface IToSqlAndFilter : ISqlFragment
    {
        string ToString(Syntax syntax);
    }
}