namespace JimTaylor1974.FilterParser
{
    public class Parameter : IParameter
    {
        public Parameter(string name)
        {
            Name = name;
        }

        public Parameter(string name, object value)
            : this(name)
        {
            Value = value;
        }

        public string Name { get; private set; }

        public object Value { get; private set; }

        public string ToString(Syntax syntax)
        {
            return syntax == Syntax.SqlKata 
                ? "?" 
                : "@" + Name;
        }
    }
}