namespace ArcCore.Utility
{
    using System;
    using System.Runtime.Serialization;


    /// <summary>
    /// A class to read a string and parse string-ified primitives
    /// </summary>
    [Obsolete]
    public class StringParserOld
    {

        public enum Status
        {
            success,
            failure_invalid_terminator,
            failure_invalid_literal
        }

        private int pos;
        private readonly string str;

        public bool autoTrim;
        public Status LastStatus { get; private set; }

        public StringParserOld(string str, int pos = 0, bool autoTrim = true)
        {
            this.str = str;
            this.pos = pos;
            this.autoTrim = autoTrim;
        }
        public void Skip(int length)
            => pos += length;
        public void SkipPast(string terminator = null)
            => pos = TerminatorOrDefaultEndIndex(terminator) + (terminator?.Length ?? 0);
        public bool ReadString(out string value, string terminator = null, bool includeTerminatorInResult = false)
        {
            int end = TerminatorOrDefaultEndIndex(terminator) + 1;
            if (end == 0)
            {
                LastStatus = Status.failure_invalid_terminator;
                value = null;
                return false;
            }

            int slen = end - pos + (includeTerminatorInResult ? terminator.Length : 0) - 1;
            value = str.Substring(pos, slen);

            if (autoTrim) value = value.Trim();

            pos = end;

            LastStatus = Status.success;
            return true;
        }

        public bool ParseFloat(out float value, string terminator = null)
        {
            value = default;
            if (!ReadString(out var v, terminator))
                return false;

            if (!float.TryParse(v, out value))
            {
                LastStatus = Status.failure_invalid_literal;
                return false;
            }

            return true;
        }
        public bool ParseInt(out int value, string terminator = null)
        {
            value = default;
            if (!ReadString(out var v, terminator))
                return false;

            if (!int.TryParse(v, out value))
            {
                LastStatus = Status.failure_invalid_literal;
                return false;
            }

            return true;
        }
        public bool ParseBool(out bool value, string terminator = null)
        {
            value = default;
            if (!ReadString(out var v, terminator))
                return false;

            if (!bool.TryParse(v, out value))
            {
                LastStatus = Status.failure_invalid_literal;
                return false;
            }

            return true;
        }

        public int TerminatorOrDefaultEndIndex(string terminator) 
            => terminator is null ? str.Length : str.IndexOf(terminator, pos);

        public string Current => str[pos].ToString();
        public string Peek(int count)
        {
            return str.Substring(pos, count);
        }
    }
}