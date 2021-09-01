using System;

namespace JimTaylor1974.FilterParser
{
    [Flags]
    public enum OperatorType
    {
        Unknown = 0,
        Whitespace = 1,
        Binary = 2, // And Or
        Logical = 4,
        Arithmetic = 8,
        Grouping = 16,
        Function = 32,
        Literal = 64
    }
}