using System;

namespace ArcCore.Parsing
{
    [Serializable]
    public class ParsingException : Exception
    {
        public static string GetMsg(string msg, int line) => $"Error at line {line}.\n\r" + msg;
        public ParsingException() { }
        public ParsingException(string message, int line) : base(GetMsg(message, line)) { }
        public ParsingException(string message, int line, Exception inner) : base(GetMsg(message, line), inner) { }
        protected ParsingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public static string GetLineEndExceptionMessage(string expectedType, string exceptTarget)
            => $"Unexpected line ending while reading {expectedType}" + (exceptTarget == null ? "." : $" within {exceptTarget}.");
        public static string GetDataEndExceptionMessage(string expectedType, string exceptTarget)
            => $"Unexpected file ending while reading {expectedType}" + (exceptTarget == null ? "." : $" within {exceptTarget}.");
        public static string GetInvalidValueExceptionMessage(string expectedType, string offendingValue, string exceptTarget)
            => $"Invalid {expectedType}: \"{offendingValue}\"" + (exceptTarget == null ? "." : $" within {exceptTarget}.");
        public static string GetUnexpectedContinuationExceptionMessage(string exceptTarget)
            => $"Expected line ending but got more data while reading a(n) {exceptTarget}";

        public static ParsingException LineEnd(string expectedType, string exceptTarget, int line)
            => new ParsingException(GetLineEndExceptionMessage(expectedType, exceptTarget), line);
        public static ParsingException DataEnd(string expectedType, string exceptTarget, int line)
            => new ParsingException(GetDataEndExceptionMessage(expectedType, exceptTarget), line);
        public static ParsingException InvalidValue(string expectedType, string offendingValue, string exceptTarget, int line)
            => new ParsingException(GetInvalidValueExceptionMessage(expectedType, offendingValue, exceptTarget), line);
        public static ParsingException UnexpectedContinuation(string exceptTarget, int line)
            => new ParsingException(GetUnexpectedContinuationExceptionMessage(exceptTarget), line);
    }
}
