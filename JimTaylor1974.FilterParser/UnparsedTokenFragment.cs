namespace JimTaylor1974.FilterParser
{
    internal class UnparsedTokenFragment : ISqlFragment
    {
        public Token Token { get; private set; }

        public UnparsedTokenFragment(Token token)
        {
            Token = token;
        }

        public override string ToString()
        {
            return "unparsed:" + Token.Value;
        }
    }
}