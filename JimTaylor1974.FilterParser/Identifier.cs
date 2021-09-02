namespace JimTaylor1974.FilterParser
{
    public class Identifier : IIdentifier
    {
        private readonly string identifier;
        private readonly string identifierRaw;
        
        public Identifier(string fieldName)
        {
            identifier = $"{fieldName.SurroundWithSquareBrackets()}";
            identifierRaw = $"{fieldName}";
        }

        public Identifier(string nameOrAlias, string fieldName)
        {
            identifier = $"{nameOrAlias.SurroundWithSquareBrackets()}.{fieldName.SurroundWithSquareBrackets()}";
            identifierRaw = $"{nameOrAlias}.{fieldName}";
        }

        public string ToString(bool rawValue)
        {
            return rawValue 
                ? identifierRaw 
                : identifier;
        }

        public override string ToString()
        {
            return identifier;
        }
    }
}