namespace ArcCore.Utilities
{
    using System;
    using Impl = System.Runtime.CompilerServices.MethodImplAttribute;
    using static System.Runtime.CompilerServices.MethodImplOptions;
    using System.Linq;

    public class StringParserNew
    {
        private readonly string value;
        private int pos;

        public static readonly string[] LineEnd = { "\n\r", "\r\n", "\n", "\r"};
        public static readonly string[] MidSpace = {" ", "\t"};
        public static readonly string[] Whitespace = {" ", "\t", "\n", "\r"};

        public bool EOSReached => pos >= value.Length;
        public char Current => value[pos];

        #region ERRORS
        [Serializable]
        public class BadParseException : Exception
        {
            static string GetMessage(int ch, StringParserNew p, string message)
            {
                string m = message + "\n";
                int ldpos = ch < p.value.Length ? ch : p.value.Length;
                while (0 < ldpos && ch - 20 < ldpos && p.value[ldpos] != '\n' && p.value[ldpos] != '\r') ldpos--;
                string n = p.value.Substring(ldpos, (ldpos + 40 >= p.value.Length) ? p.value.Length - ldpos : ldpos + 40);
                for(int i = 0; i < n.Length; i++)
                {
                    if (n[i] == '\n' || n[i] == '\r' || n[i] == '\t') m += '#';
                    else m += n[i];
                }
                m += new string(' ', ch - ldpos);
                m += "^";
                return m;
            }
            private protected BadParseException(int character, StringParserNew parser, string message)
                : base(GetMessage(character, parser, message))
            {}
        }
        [Serializable]
        public sealed class UnexpectedEOSException : BadParseException
        {
            internal UnexpectedEOSException(int character, StringParserNew parser, string message)
                : base(character, parser, message) {}
        }
        [Serializable]
        public sealed class UnexpectedCharException : BadParseException
        {
            internal UnexpectedCharException(int character, StringParserNew parser, string message)
                : base(character, parser, message) { }
        }
        [Serializable]
        public sealed class BadLiteralException : BadParseException
        {
            internal BadLiteralException(int character, StringParserNew parser, string message)
                : base(character, parser, message) { }
        }
        #endregion

        public StringParserNew(string value)
        {
            this.value = value ?? throw new ArgumentNullException(nameof(value));
            pos = 0;
        }

        public string Peek(int amount)
            => value.Substring(pos, amount);

        public void Skip(int amount, string exceptionMessage = null)
        {
            pos += amount;
            if (EOSReached) throw new UnexpectedEOSException(pos, null, exceptionMessage ?? "End of string reached.");
        }
        public void SkipPast(string end, string exceptionMessage = null)
        {
            int subCnt = 0;

            while (!EOSReached && Current != end[end.Length - 1])
            {
                if (Current == end[subCnt]) subCnt++;
                else subCnt = 0;
                pos++;
            }

            if (EOSReached) throw new UnexpectedEOSException(pos, this, exceptionMessage ?? "End of string found before end.");
        }
        public void SkipPast(string[] ends, out int which, string exceptionMessage = null)
        {
            which = default;
            int[] subCnts = new int[ends.Length];

            while(!EOSReached)
            {
                for(int i = 0; i < ends.Length; i++)
                {
                    if (subCnts[i] < ends[i].Length && Current == ends[i][subCnts[i]]) subCnts[i]++;
                    else subCnts[i] = 0;

                    if (subCnts[i] == ends[i].Length)
                    {
                        which = i;
                        return;
                    }
                }

                pos++;

                if (EOSReached) throw new UnexpectedEOSException(pos, this, exceptionMessage ?? "End of string found before end.");
            }
        }
        public void SkipPast(string[] ends, string exceptionMessage = null)
            => SkipPast(ends, out _, exceptionMessage);

        public void SkipPastAll(string[] ends, string exceptionMessage = null)
        {
            while(!ends.Any((string s) => Current == s[0]))
            {
                SkipPast(ends, exceptionMessage);
            }
        }

        public string GetSection(int length, string exceptionMessage = null)
            => pos + length < value.Length ? value.Substring(pos, pos += length) : throw new UnexpectedEOSException(pos, this, exceptionMessage ?? "Section too long.");
        public string GetSection(string end, bool includeEnd = false, bool trim = true, string exceptionMessage = null)
        {
            if(end is null) throw new ArgumentNullException(nameof(end));

            int oldPos = pos;
            SkipPast(end, exceptionMessage);

            string s = value.Substring(oldPos, pos++);

            if (!includeEnd) s = s.Substring(0, s.Length - end.Length);
            if (trim) s = s.Trim();

            return s;
        }
        public string GetSection(string[] ends, bool includeEnd = false, bool trim = true, string exceptionMessage = null)
        {
            if (ends is null) throw new ArgumentNullException(nameof(ends));

            int oldPos = pos;
            SkipPast(ends, out int which, exceptionMessage);

            string s = value.Substring(oldPos, pos++);

            if (!includeEnd) s = s.Substring(0, s.Length - ends[which].Length);
            if (trim) s = s.Trim();

            return s;
        }

        public void SkipWhitespace(bool permitEOS = false, string exceptionMessage = null)
        {
            while (!EOSReached && char.IsWhiteSpace(Current)) pos++;
            if (!permitEOS && EOSReached) throw new UnexpectedEOSException(pos, this, exceptionMessage ?? "End of string reached when skipping whitespace.");
        }

        public void Require(string value, bool trim = true, string exceptionMessage = null)
        {
            if(trim)
            {
                SkipWhitespace();
            }

            for(int i = 0; i < value.Length; i++)
            {
                if (Current != value[i]) throw new UnexpectedCharException(pos, this, exceptionMessage ?? "Required string is malformed or missing.");
                pos++;
            }
        }
        public void Require(string[] values, bool trim = true, string exceptionMessage = null)
        {
            int oldPos = pos;
            int mlen = values.Max((string s) => s.Length);
            bool found = false;

            for(int i = 0; i < values.Length; i++)
            {
                try
                {
                    if (found && values[i].Length == mlen) return;
                    Require(values[i], trim);
                }
                catch(Exception)
                {
                    pos = oldPos;
                    continue;
                }

                found = true;
                if (values[i].Length == mlen) return;
            }

            throw new UnexpectedCharException(pos, this, exceptionMessage ?? "Required string is malformed or missing");
        }

        public float GetFloat(int length, string exceptionMessage = null)
        {
            string s = GetSection(length, exceptionMessage);
            return _GetFloat(s, exceptionMessage);
        }
        public float GetFloat(string end, bool trim = true, string exceptionMessage = null)
        {
            string s = GetSection(end, false, trim, exceptionMessage);
            return _GetFloat(s, exceptionMessage);
        }
        public float GetFloat(string[] ends, bool trim = true, string exceptionMessage = null)
        {
            string s = GetSection(ends, false, trim, exceptionMessage);
            return _GetFloat(s, exceptionMessage);
        }

        [Impl(AggressiveInlining)] private float _GetFloat(string s, string exceptionMessage)
        {
            if (float.TryParse(s, out var result))
                return result;
            throw new BadLiteralException(pos - s.Length, this, exceptionMessage ?? "Invalid float literal.");
        }

        public int GetInt(int length, string exceptionMessage = null)
        {
            string s = GetSection(length, exceptionMessage);
            return _GetInt(s, exceptionMessage);
        }
        public int GetInt(string end, bool trim = true, string exceptionMessage = null)
        {
            string s = GetSection(end, false, trim, exceptionMessage);
            return _GetInt(s, exceptionMessage);
        }
        public int GetInt(string[] ends, bool trim = true, string exceptionMessage = null)
        {
            string s = GetSection(ends, false, trim, exceptionMessage);
            return _GetInt(s, exceptionMessage);
        }

        [Impl(AggressiveInlining)] private int _GetInt(string s, string exceptionMessage)
        {
            if (int.TryParse(s, out var result))
                return result;
            throw new BadLiteralException(pos - s.Length, this, exceptionMessage ?? "Invalid integer literal.");
        }

        public bool GetBool(int length, string exceptionMessage = null)
        {
            string s = GetSection(length, exceptionMessage);
            return _GetBool(s, exceptionMessage);
        }
        public bool GetBool(string end, bool trim = true, string exceptionMessage = null)
        {
            string s = GetSection(end, false, trim, exceptionMessage);
            return _GetBool(s, exceptionMessage);
        }
        public bool GetBool(string[] ends, bool trim = true, string exceptionMessage = null)
        {
            string s = GetSection(ends, false, trim, exceptionMessage);
            return _GetBool(s, exceptionMessage);
        }

        [Impl(AggressiveInlining)] private bool _GetBool(string s, string exceptionMessage)
        {
            if (bool.TryParse(s, out var result))
                return result;
            throw new BadLiteralException(pos - s.Length, this, exceptionMessage ?? "Invalid boolean literal.");
        }
    }
}