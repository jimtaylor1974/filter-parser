namespace JimTaylor1974.FilterParser
{
    public class Separator
    {
        private readonly string separator;
        private bool first = true;

        public Separator(string separator)
        {
            this.separator = separator;
        }

        public string Value()
        {
            if (first)
            {
                first = false;
                return string.Empty;
            }

            return separator;
        }
    }
}