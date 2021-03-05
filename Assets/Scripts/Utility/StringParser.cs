namespace ArcCore.Utility
{
    /// <summary>
    /// A class to read a string and parse string-ified primitives
    /// </summary>
    public class StringParser
    {
        private int pos;
        private readonly string str;
        public StringParser(string str)
        {
            this.str = str;
        }
        public void Skip(int length)
            => pos += length;
        public void SkipPast(string terminator = null)
            => pos += TerminatorOrDefaultEndIndex(terminator) - pos + 1;
        public bool ParseFloat(out float value, string terminator = null)
        {
            int end = terminator is null ? str.Length : str.IndexOf(terminator, pos);
            bool success = float.TryParse(str.Substring(pos, end - pos), out value);
            pos += end - pos + 1;
            return success;
        }
        public bool ParseInt(out int value, string terminator = null)
        {
            int end = TerminatorOrDefaultEndIndex(terminator);
            bool success = int.TryParse(str.Substring(pos, end - pos), out value);
            pos += end - pos + 1;
            return success;
        }
        public bool ParseBool(out bool value, string terminator = null)
        {
            int end = TerminatorOrDefaultEndIndex(terminator);
            bool success = bool.TryParse(str.Substring(pos, end - pos), out value);
            pos += end - pos + 1;
            return success;
        }
        public string ReadString(string terminator = null)
        {
            int end = TerminatorOrDefaultEndIndex(terminator);
            string value = str.Substring(pos, end - pos);
            pos += end - pos + 1;
            return value;
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