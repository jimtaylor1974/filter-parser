namespace JimTaylor1974.FilterParser
{
    public class Identifier : IIdentifier
    {
        private readonly string identifier;

        public Identifier(string fieldName)
        {
            identifier = $"{fieldName.SurroundWithSquareBrackets()}";
        }

        public Identifier(string nameOrAlias, string fieldName)
        {
            identifier = $"{nameOrAlias.SurroundWithSquareBrackets()}.{fieldName.SurroundWithSquareBrackets()}";
        }

        public override string ToString()
        {
            return identifier;
        }
    }
}