namespace JimTaylor1974.FilterParser
{
    public class ToSqlAndFilter : IToSqlAndFilter
    {
        private readonly string filter;
        private readonly string sql;

        public ToSqlAndFilter(string filter, string sql)
        {
            this.filter = filter;
            this.sql = sql;
        }

        public string ToString(Syntax syntax)
        {
            return syntax == Syntax.Filter ? filter : sql;
        }
    }
}