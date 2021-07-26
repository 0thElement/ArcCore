using System;
using System.IO;
using System.Text;

namespace ArcCore.Parsing
{
    public class CommandParser
    {
        protected class CommandData
        {
            public string contextName, matchFailureMessage;
            public Action commandAction;
            public Func<bool> match;

            public void Deconstruct(out string contextName, out Action commandAction, out string matchFailureMessage, out Func<bool> match)
            {
                contextName = this.contextName;
                commandAction = this.commandAction;
                matchFailureMessage = this.matchFailureMessage;
                match = this.match;
            }

            public static implicit operator CommandData((string, Action) tuple)
                => new CommandData
                {
                    contextName = tuple.Item1,
                    commandAction = tuple.Item2,
                    matchFailureMessage = null,
                    match = (() => true)
                };

            public static implicit operator CommandData((string, Action, string, Func<bool>) tuple)
                => new CommandData
                {
                    contextName = tuple.Item1,
                    commandAction = tuple.Item2,
                    matchFailureMessage = tuple.Item3,
                    match = tuple.Item4
                };
        }

        public delegate bool TryConverter(string value, out object result);
        public delegate bool TryConverter<T>(string value, out T result);

        public CommandParser(string[] lines)
            => this.lines = lines;

        public static CommandParser FromFile(string path)
            => new CommandParser(File.ReadAllLines(path));
        public static CommandParser FromFile(string path, Encoding encoding)
            => new CommandParser(File.ReadAllLines(path, encoding));

        public string[] lines;
        private int lineIdx, charIdx;

        protected string currentContext;

        public int RealLineIndex => lineIdx + 1;

        public bool LineIsUnfinished => charIdx < lines[lineIdx].Length;
        public bool IsFinished => lineIdx >= lines.Length;

        public string CurrentLine => lines[lineIdx];
        public char Current => lines[lineIdx][charIdx];

        public Predicate<char> SeperatorPredicate { get; protected set; } = char.IsWhiteSpace;
        public char OpenStringChar { get; protected set; } = '"';
        public char CloseStringChar { get; protected set; } = '"';

        public ParsingException GetParsingException(string exceptMsg)
            => new ParsingException(exceptMsg, RealLineIndex);

        private protected virtual CommandData GetCommandData(string command) => null;

        public bool ExecuteCommand()
        {
            if (!StartCommand())
                return false;

            var cmdRaw = GetValue("command");
            var cmd = GetCommandData(cmdRaw);

            if (cmd == null)
                throw GetParsingException($"Invalid command for a parser of type {GetType().Name}: \"{cmdRaw}\".");

            if (!cmd.match())
                throw GetParsingException(cmd.matchFailureMessage);

            currentContext = string.IsNullOrEmpty(cmd.contextName) ? cmd.contextName : cmdRaw;
            Exception e = null;

            try
            {
                cmd.commandAction();
            }
            catch(Exception cmdE)
            {
                e = new Exception($"An error occured while executing command \"{cmdRaw}\": {cmdE}. " +
                    $"This may be due to an unspecified predicate failure, or faulty user code.");
            }

            EndCommand();

            if (e != null) throw e;
            else return true;
        }

        public void Execute()
        {
            while (ExecuteCommand()) ;
        }

        public void NextLine()
        {
            lineIdx++;
            charIdx = 0;
        }

        private bool SkipAhead()
        {
            while (SeperatorPredicate(Current))
            {
                charIdx++;

                if (!LineIsUnfinished)
                    return false;
            }

            return true;
        }

        private bool StartCommand()
        {
            while (!IsFinished && string.IsNullOrWhiteSpace(CurrentLine))
            {
                NextLine();
            }
            return !IsFinished;
        }

        private void EndCommand()
        {
            while (LineIsUnfinished)
            {
                if (!SeperatorPredicate(Current))
                    throw ParsingException.UnexpectedContinuation(currentContext, RealLineIndex);

                charIdx++;
            }

            NextLine();
            currentContext = null;
        }

        private string GetSection()
        {
            int startIdx = charIdx;

            while (LineIsUnfinished && !SeperatorPredicate(Current))
                charIdx++;

            if (LineIsUnfinished) return lines[lineIdx].Substring(startIdx, charIdx - startIdx);
            else return lines[lineIdx].Substring(startIdx);
        }

        protected bool GetValue(out string value, string expectedType)
        {
            if (!SkipAhead())
            {
                value = null;
                return false;
            }

            value = GetSection();
            return true;
        }
        protected string GetValue(string expectedType)
        {
            if (!SkipAhead())
                throw ParsingException.LineEnd(expectedType, currentContext, RealLineIndex);

            return GetSection();
        }

        private string GetStrValueUnderhood()
        {
            if (Current == OpenStringChar)
            {
                StringBuilder str = new StringBuilder();

                while (!IsFinished)
                {
                    int incrCount = 1;

                    if (Current == CloseStringChar)
                    {
                        if (CurrentLine.Length > charIdx && CurrentLine[charIdx + 1] != CloseStringChar)
                        {
                            str.Append(CloseStringChar);
                            incrCount = 2;
                        }
                        else break;
                    }

                    charIdx += incrCount;
                    if (!LineIsUnfinished)
                    {
                        NextLine();
                        str.Append(Environment.NewLine);
                    }
                    else
                    {
                        str.Append(Current);
                    }
                }

                if (!LineIsUnfinished)
                    throw ParsingException.DataEnd("string", currentContext, RealLineIndex);

                return str.ToString();
            }
            else return GetSection();
        }

        protected bool GetStrValue(out string value)
        {
            if (!SkipAhead())
            {
                value = null;
                return false;
            }

            value = GetStrValueUnderhood();
            return true;
        }

        protected string GetStrValue()
        {
            if (!SkipAhead())
                throw ParsingException.LineEnd("string", currentContext, RealLineIndex);

            return GetStrValueUnderhood();
        }

        protected T GetTypedValue<T>(string expectedTypeName, TryConverter<T> parser, Predicate<T> predicate = null)
        {
            var value = GetValue(expectedTypeName);

            if (!parser(value, out var result) || (predicate != null && predicate(result)))
                throw ParsingException.InvalidValue(expectedTypeName, value, currentContext, RealLineIndex);

            return result;

        }

        protected bool GetTypedValue<T>(out T value, string expectedTypeName, TryConverter<T> parser, Predicate<T> predicate = null)
        {
            if (!GetValue(out var rvalue, expectedTypeName))
            {
                value = default;
                return false;
            }

            if (!parser(rvalue, out value) || (predicate != null && predicate(value)))
                throw ParsingException.InvalidValue(expectedTypeName, rvalue, currentContext, RealLineIndex);

            return true;
        }

        #region Convenience Wrappers
        protected int GetInt(string expectedType = "integer", Predicate<int> predicate = null)
            => GetTypedValue<int>(expectedType, int.TryParse, predicate);
        protected bool GetInt(out int value, string expectedType = "integer", Predicate<int> predicate = null)
            => GetTypedValue<int>(out value, expectedType, int.TryParse, predicate);


        protected float GetFloat(string expectedType = "float", Predicate<float> predicate = null)
            => GetTypedValue<float>(expectedType, float.TryParse, predicate);
        protected bool GetFloat(out float value, string expectedType = "float", Predicate<float> predicate = null)
            => GetTypedValue<float>(out value, expectedType, float.TryParse, predicate);
        #endregion
    }
}
