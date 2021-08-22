using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ArcCore.Parsing
{
    public abstract class CommandParser
    {
        protected class Command
        {
            public string context;
            public Action commandAction;
            public bool predicate;
            public string predicateMessage;

            public static implicit operator Command((string, Action) tpl)
                => new Command
                {
                    context = tpl.Item1,
                    commandAction = tpl.Item2,
                    predicate = true,
                    predicateMessage = null
                };
            public static implicit operator Command((string, Action, bool, string) tpl)
                => new Command
                {
                    context = tpl.Item1,
                    commandAction = tpl.Item2,
                    predicate = tpl.Item3,
                    predicateMessage = tpl.Item4
                };
        }

        public delegate bool TryConverter(string value, out object result);
        public delegate bool TryConverter<T>(string value, out T result);

        public CommandParser(string[] lines)
        {
            this.lines = lines;
            currentContext = new List<string>();
        }
        public CommandParser(CommandParser parser) : this(parser.lines) {}

        private readonly string[] lines;
        private int lineIdx, charIdx;
        private List<string> currentContext;
        private int addedContextCount;

        private int RealLineIndex => lineIdx + 1;

        private bool LineIsUnfinished => charIdx < lines[lineIdx].Length;
        private bool IsFinished => lineIdx >= lines.Length;

        private string CurrentLine => lines[lineIdx];
        private char Current => lines[lineIdx][charIdx];

        protected readonly Predicate<char> seperatorPredicate = char.IsWhiteSpace;
        protected readonly char openStringChar = '"';
        protected readonly char closeStringChar = '"';

        protected ParsingException GetParsingException(string exceptMsg)
            => new ParsingException(exceptMsg, RealLineIndex);

        protected void AddContext(string context)
        {
            if (string.IsNullOrWhiteSpace(context)) return;

            addedContextCount++;
            currentContext.Add(context);
        }

        protected void AddPermanentContext(string context)
        {
            if (string.IsNullOrWhiteSpace(context)) return;

            currentContext.Add(context);
        }

        protected void RemoveContext(int count = 1)
        {
            addedContextCount -= count;
            while (count-- > 0)
                currentContext.RemoveAt(currentContext.Count - 1);
        }

        protected void ClearContext()
        {
            addedContextCount = 0;
            currentContext.Clear();
        }

        protected void Require(bool isTrue, string message)
        {
            if (!isTrue)
                throw GetParsingException(message);
        }

        protected void InvalidCommand(string command)
        {
            throw GetParsingException($"Invalid command: \"{command}\".");
        }

        private protected abstract Command ExecuteCommand(string command);

        public bool ExecuteSingle()
        {
            if (!StartCommand())
                return false;

            var cmdRaw = GetValue("command");
            var command = ExecuteCommand(cmdRaw);

            if (command == null)
                InvalidCommand(cmdRaw);

            if (!command.predicate)
                throw GetParsingException(command.predicateMessage);

            if (command.context != null)
                AddContext(command.context);

            Exception e = null;

            try
            {
                command.commandAction();
            }
            catch(Exception cmdE)
            {
                if (cmdE is ParsingException)
                    e = cmdE;
                else 
                    e = new Exception($"An error occured while executing command \"{cmdRaw}\": {cmdE}. " +
                    $"This may be due to an unspecified predicate failure, or faulty user code.");
            }

            if (e != null) throw e;

            EndCommand();
            return true;
        }

        public void Execute()
        {
            OnExecutionStartup();
            while (ExecuteSingle()) ;
            OnExecutionEnd();
        }

        private protected virtual void OnExecutionStartup() { }
        private protected virtual void OnExecutionEnd() { }

        public void NextLine()
        {
            lineIdx++;
            charIdx = 0;
        }

        private bool SkipAhead()
        {
            while (seperatorPredicate(Current))
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
                if (!seperatorPredicate(Current))
                {
                    Debug.Log($"{CurrentLine} @ {charIdx}; {Current}");
                    throw ParsingException.UnexpectedContinuation(currentContext, RealLineIndex);
                }

                charIdx++;
            }

            NextLine();
            RemoveContext(addedContextCount);
        }

        private string GetSection()
        {
            int startIdx = charIdx;

            while (LineIsUnfinished && !seperatorPredicate(Current))
                charIdx++;

            string val;

            if (LineIsUnfinished) val = lines[lineIdx].Substring(startIdx, charIdx - startIdx);
            else val = lines[lineIdx].Substring(startIdx);

            //Debug.Log(val);
            return val;
        }

        private bool GetValueBase(out string value, string expectedType)
        {
            AddContext(expectedType);

            if (!SkipAhead())
            {
                value = null;
                return false;
            }

            value = GetSection();
            return true;
        }
        private string GetValueBase(string expectedType)
        {
            AddContext(expectedType);

            if (!SkipAhead())
                throw ParsingException.LineEnd(currentContext, RealLineIndex);

            return GetSection();
        }

        protected bool GetValue(out string value, string expectedType)
        {
            var b = GetValueBase(out value, expectedType);
            RemoveContext();
            return b;
        }
        protected string GetValue(string expectedType)
        {
            var r = GetValueBase(expectedType);
            RemoveContext();
            return r;
        }

        private string GetStrValueUnderhood()
        {
            if (Current == openStringChar)
            {
                StringBuilder str = new StringBuilder();

                while (!IsFinished)
                {
                    int incrCount = 1;

                    if (Current == closeStringChar)
                    {
                        if (CurrentLine.Length > charIdx && CurrentLine[charIdx + 1] != closeStringChar)
                        {
                            str.Append(closeStringChar);
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
                    throw ParsingException.DataEnd(currentContext, RealLineIndex);

                return str.ToString();
            }
            else
            {
                return GetSection();
            }
        }

        protected string GetStrValue()
        {
            AddContext("string");

            if (!SkipAhead())
            {
                var newContext = new List<string>(currentContext);
                RemoveContext();
                throw ParsingException.LineEnd(newContext, RealLineIndex);
            }

            RemoveContext();

            return GetStrValueUnderhood();
        }

        protected bool GetStrValue(out string value)
        {
            AddContext("string");

            if (!SkipAhead())
            {
                value = null;
                return false;
            }

            RemoveContext();

            value = GetStrValueUnderhood();
            return true;
        }

        protected T GetTypedValue<T>(string expectedTypeName, TryConverter<T> parser, Predicate<T> predicate = null)
        {
            var value = GetValueBase(expectedTypeName);

            if (!parser(value, out var result) || (predicate != null && !predicate(result)))
                throw ParsingException.InvalidValue(currentContext, value, RealLineIndex);

            RemoveContext();
            return result;
        }

        protected bool GetTypedValue<T>(out T value, string expectedTypeName, TryConverter<T> parser, Predicate<T> predicate = null)
        {
            if (!GetValueBase(out var rvalue, expectedTypeName))
            {
                value = default;
                return false;
            }

            if (!parser(rvalue, out value) || (predicate != null && !predicate(value)))
            {
                return false;
            }

            RemoveContext();
            return true;
        }

        #region Convenience Wrappers
        protected int GetInt(string expectedType = "integer", Predicate<int> predicate = null)
            => GetTypedValue(expectedType, int.TryParse, predicate);
        protected bool GetInt(out int value, string expectedType = "integer", Predicate<int> predicate = null)
            => GetTypedValue(out value, expectedType, int.TryParse, predicate);

        protected float GetFloat(string expectedType = "float", Predicate<float> predicate = null)
            => GetTypedValue(expectedType, float.TryParse, predicate);
        protected bool GetFloat(out float value, string expectedType = "float", Predicate<float> predicate = null)
            => GetTypedValue(out value, expectedType, float.TryParse, predicate);

        protected bool GetBool(string expectedType = "float", Predicate<bool> predicate = null)
            => GetTypedValue(expectedType, bool.TryParse, predicate);
        protected bool GetBool(out bool value, string expectedType = "float", Predicate<bool> predicate = null)
            => GetTypedValue(out value, expectedType, bool.TryParse, predicate);
        #endregion
    }
}
