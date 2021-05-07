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
        public Status LastStatus { get; private set; }
        public StringParserOld(string str)
        {
            this.str = str;
        }
        public void Skip(int length)
            => pos += length;
        public void SkipPast(string terminator = null)
            => pos += TerminatorOrDefaultEndIndex(terminator) - pos + 1;
        public bool ParseFloat(out float value, string terminator = null)
        {
            value = default;
            int end = (str?.Length ?? str.IndexOf(terminator, pos));

            if (end == -1)
            {
                LastStatus = Status.failure_invalid_terminator;
                return false;
            }

            if (!float.TryParse(str.Substring(pos, end - pos), out value))
            {
                LastStatus = Status.failure_invalid_literal;
                return false;
            }

            pos = end - 1;

            LastStatus = Status.success;
            return true;
        }
        public bool ParseInt(out int value, string terminator = null)
        {
            value = default;
            int end = terminator is null ? str.Length : str.IndexOf(terminator, pos);

            if (end == -1)
            {
                LastStatus = Status.failure_invalid_terminator;
                return false;
            }

            if (!int.TryParse(str.Substring(pos, end - pos), out value))
            {
                LastStatus = Status.failure_invalid_literal;
                return false;
            }

            pos += end - pos + 1;

            LastStatus = Status.success;
            return true;
        }
        public bool ParseBool(out bool value, string terminator = null)
        {
            value = default;
            int end = terminator is null ? str.Length : str.IndexOf(terminator, pos);

            if (end == -1)
            {
                LastStatus = Status.failure_invalid_terminator;
                return false;
            }

            if (!bool.TryParse(str.Substring(pos, end - pos), out value))
            {
                LastStatus = Status.failure_invalid_literal;
                return false;
            }

            pos += end - pos + 1;

            LastStatus = Status.success;
            return true;
        }
        public bool ReadString(out string value, string terminator = null)
        {
            value = null;
            int end = TerminatorOrDefaultEndIndex(terminator);

            if (end == -1)
            {
                LastStatus = Status.failure_invalid_terminator;
                return false;
            }

            value = str.Substring(pos, end - pos);
            pos += end - pos + 1;

            LastStatus = Status.success;
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

namespace ArcCore.Utility
{
}