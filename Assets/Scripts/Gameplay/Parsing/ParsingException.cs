using System;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Parsing
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

        private static string GetMessageEnd(List<string> context)
            => context.Count == 0 ? "." : $" while reading {string.Join(" :: ", context)}.";

        public static string GetLineEndExceptionMessage(List<string> context)
            => $"Unexpected line ending" + GetMessageEnd(context);
        public static string GetDataEndExceptionMessage(List<string> context)
            => $"Unexpected file ending" + GetMessageEnd(context);
        public static string GetInvalidValueExceptionMessage(List<string> context, string offendingValue)
            => $"Invalid value \"{offendingValue}\"" + GetMessageEnd(context);
        public static string GetUnexpectedContinuationExceptionMessage(List<string> context)
            => $"Expected line ending but got more data" + GetMessageEnd(context);

        public static ParsingException LineEnd(List<string> context, int line)
            => new ParsingException(GetLineEndExceptionMessage(context), line);
        public static ParsingException DataEnd(List<string> context, int line)
            => new ParsingException(GetDataEndExceptionMessage(context), line);
        public static ParsingException InvalidValue(List<string> context, string offendingValue, int line)
            => new ParsingException(GetInvalidValueExceptionMessage(context, offendingValue), line);
        public static ParsingException UnexpectedContinuation(List<string> context, int line)
            => new ParsingException(GetUnexpectedContinuationExceptionMessage(context), line);
    }
}
