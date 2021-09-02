namespace JimTaylor1974.FilterParser
{
    public interface IIdentifier : ISqlFragment
    {
        string ToString(bool rawValue);
    }
}