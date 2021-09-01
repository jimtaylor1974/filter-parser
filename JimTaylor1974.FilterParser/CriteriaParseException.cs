using System;
using System.Runtime.Serialization;

namespace JimTaylor1974.FilterParser
{
    [Serializable]
    public class CriteriaParseException : Exception
    {
        protected CriteriaParseException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        public CriteriaParseException()
        {
        }

        public CriteriaParseException(string message) : base(message)
        {
        }

        public CriteriaParseException(Exception exception) : base(exception.Message, exception)
        {
        }
    }
}
