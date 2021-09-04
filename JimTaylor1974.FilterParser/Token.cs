/********************************************************
 *	Author: Andrew Deren
 *	Date: July, 2004
 *	http://www.adersoftware.com
 * 
 *	StringTokenizer class. You can use this class in any way you want
 * as long as this header remains in this file.
 * 
 **********************************************************/

namespace JimTaylor1974.FilterParser
{
    public enum TokenKind
    {
        Unknown,
        Word,
        Number,
        QuotedString,
        WhiteSpace,
        Symbol,
        EOL,
        EOF
    }

    public class Token
    {
        public Token(TokenKind kind, string value, int line, int column)
        {
            this.Kind = kind;
            this.Value = value;
            this.Line = line;
            this.Column = column;
        }

        public int Column { get; }

        public TokenKind Kind { get; }

        public int Line { get; }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}