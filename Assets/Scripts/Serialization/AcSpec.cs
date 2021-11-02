using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace AcSpec
{
    public class AcObject : IDictionary<string, object>
    {
        private Dictionary<string, object> dictionary = new Dictionary<string, object>();

        #region Implementation of IDictionary<string, object>
        public object this[string key] { get => ((IDictionary<string, object>)dictionary)[key]; set => ((IDictionary<string, object>)dictionary)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, object>)dictionary).Keys;

        public ICollection<object> Values => ((IDictionary<string, object>)dictionary).Values;

        public int Count => ((ICollection<KeyValuePair<string, object>>)dictionary).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, object>>)dictionary).IsReadOnly;

        public void Add(string key, object value)
        {
            ((IDictionary<string, object>)dictionary).Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)dictionary).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, object>>)dictionary).Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)dictionary).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, object>)dictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)dictionary).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, object>)dictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)dictionary).Remove(item);
        }

        public bool TryGetValue(string key, out object value)
        {
            return ((IDictionary<string, object>)dictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)dictionary).GetEnumerator();
        }
        #endregion

        public object RequireNested(string path)
        {
            string[] names = path.Split('.');
            var cur = this;

            for (int i = 0; i < names.Length - 1; i++)
            {
                cur = cur.Require<AcObject>(names[i]);
            }

            return cur.Require(names.Last());
        }

        public T RequireNested<T>(string path)
        {
            if (RequireNested(path) is T t)
            {
                return t;
            }
            else throw new Exception($"Value of name \"{path}\" was found but was invalid for the type \"{typeof(T)}\"");
        }

        public object Require(string name)
        {
            if (dictionary.ContainsKey(name))
            {
                return dictionary[name];
            }
            else throw new Exception($"Value of name \"{name}\" was not found");
        }

        public T Require<T>(string name)
        {
            if (Require(name) is T t)
            {
                return t;
            }
            else throw new Exception($"Value of name \"{name}\" was found but was invalid for the type \"{typeof(T)}\"");
        }

        public void Merge(AcObject from)
        {
            foreach (var k in from.Keys)
            {
                if (!ContainsKey(k))
                {
                    Add(k, from[k]);
                }
            }
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", this) + "}";
        }
    }

    public class AcList : IList<object>
    {
        private List<object> _list = new List<object>();

        #region Implementation of IList<object>
        public object this[int index] { get => ((IList<object>)_list)[index]; set => ((IList<object>)_list)[index] = value; }

        public int Count => ((ICollection<object>)_list).Count;

        public bool IsReadOnly => ((ICollection<object>)_list).IsReadOnly;

        public void Add(object item)
        {
            ((ICollection<object>)_list).Add(item);
        }

        public void Clear()
        {
            ((ICollection<object>)_list).Clear();
        }

        public bool Contains(object item)
        {
            return ((ICollection<object>)_list).Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            ((ICollection<object>)_list).CopyTo(array, arrayIndex);
        }

        public IEnumerator<object> GetEnumerator()
        {
            return ((IEnumerable<object>)_list).GetEnumerator();
        }

        public int IndexOf(object item)
        {
            return ((IList<object>)_list).IndexOf(item);
        }

        public void Insert(int index, object item)
        {
            ((IList<object>)_list).Insert(index, item);
        }

        public bool Remove(object item)
        {
            return ((ICollection<object>)_list).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<object>)_list).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
        #endregion

        public override string ToString()
        {
            return "[" + string.Join(", ", this) + "]";
        }
    }

    public readonly struct AcColor
    {
        public readonly byte r, g, b, a;
        public AcColor(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public override string ToString()
            => $"#[{r}, {g}, {b}, {a}]#";
    }

    internal enum TokenType
    {
        Identifier,
        VariableRead,

        Number,
        String,
        Color,

        Eq,
        Comma,

        OpenList,
        CloseList,

        OpenObject,
        CloseObject,

        EOS
    }

    internal readonly struct Token
    {
        public readonly TokenType type;
        public readonly object value;

        public Token(TokenType type, object value)
        {
            this.type = type;
            this.value = value;
        }

        public Token(TokenType type) : this(type, null) { }
    }

    internal static class Lexer
    {
        internal static List<Token> Tokenize(string text)
        {
            //Tokenize
            var tokens = new List<Token>();

            const char EOS = '\x0001';

            int idx = 0;
            char GetSafe(int offset = 0) => (offset + idx >= text.Length) ? EOS : text[offset + idx];
            void Adv(int count = 1) => idx += count;

            string ReadIdentifier(bool withDot)
            {
                int stIdx = idx;

                if (!char.IsLetter(GetSafe()) && GetSafe() != '_')
                    throw new Exception($"Invalid start to an identifier: '{GetSafe()}'");
                Adv();

                while (char.IsLetterOrDigit(GetSafe()) || GetSafe() == '_' || (GetSafe() == '.' && withDot))
                    Adv();

                return text.Substring(stIdx, idx - stIdx);
            }

            while (GetSafe() != EOS)
            {
                switch (GetSafe())
                {
                    //whitespace
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                        Adv();
                        break;

                    //arrays
                    case '[':
                        tokens.Add(new Token(TokenType.OpenList));
                        Adv();
                        break;

                    case ']':
                        tokens.Add(new Token(TokenType.CloseList));
                        Adv();
                        break;

                    //objects
                    case '{':
                        tokens.Add(new Token(TokenType.OpenObject));
                        Adv();
                        break;

                    case '}':
                        tokens.Add(new Token(TokenType.CloseObject));
                        Adv();
                        break;

                    //equals sign
                    case '=':
                        tokens.Add(new Token(TokenType.Eq));
                        Adv();
                        break;

                    //comma
                    case ',':
                        tokens.Add(new Token(TokenType.Comma));
                        Adv();
                        break;

                    //comments
                    case '~':
                        while (GetSafe() != '\n' && GetSafe() != '\r' && GetSafe() != EOS)
                            Adv();
                        Adv();
                        break;

                    //color literals
                    case '#':
                        Adv();

                        bool IsValidDigit()
                        {
                            char c = char.ToLower(GetSafe());
                            if (char.IsDigit(c) || c == 'a' || c == 'b' || c == 'c' || c == 'd' || c == 'e' || c == 'f')
                                return true;

                            return false;
                        }

                        void EnsureValidDigit()
                        {
                            if (IsValidDigit())
                                return;

                            throw new Exception("Invalid hexadecimal digit in color literal");
                        }

                        byte ReadOne()
                        {
                            var stIdx = idx;

                            EnsureValidDigit();
                            Adv();
                            EnsureValidDigit();
                            Adv();

                            string substr = text.Substring(stIdx, idx - stIdx);

                            return byte.Parse(substr, System.Globalization.NumberStyles.HexNumber);
                        }

                        byte r = ReadOne();
                        byte g = ReadOne();
                        byte b = ReadOne();
                        byte a = 0xFF;

                        if (IsValidDigit())
                            a = ReadOne();

                        tokens.Add(new Token(TokenType.Color, new AcColor(r, g, b, a)));
                        break;

                    //strings
                    case '"':
                        Adv();

                        StringBuilder cur = new StringBuilder();

                        while (GetSafe() != '"')
                        {
                            switch (GetSafe())
                            {
                                case '\\':
                                    Adv();
                                    switch (GetSafe())
                                    {
                                        case '\\':
                                            cur.Append('\\');
                                            break;
                                        case 'n':
                                            cur.Append('\n');
                                            break;
                                        case 'r':
                                            cur.Append('\r');
                                            break;
                                        case 't':
                                            cur.Append('\t');
                                            break;
                                        case '"':
                                            cur.Append('"');
                                            break;
                                        default:
                                            throw new Exception($"Unrecognized escape sequence: \"\\{GetSafe()}\"");
                                    }
                                    Adv();
                                    break;

                                case EOS:
                                    throw new Exception("Unclosed string literal at end of file.");

                                default:
                                    cur.Append(GetSafe());
                                    Adv();
                                    break;
                            }
                        }

                        Adv();

                        tokens.Add(new Token(TokenType.String, cur.ToString()));
                        break;

                    //variable reference
                    case '%':
                        Adv();
                        tokens.Add(new Token(TokenType.VariableRead, ReadIdentifier(withDot: true)));
                        break;

                    //global references
                    default:

                        if (char.IsDigit(GetSafe()))
                        {
                            var stIdx = idx;
                            while (char.IsDigit(GetSafe()) || GetSafe() == '.')
                                Adv();

                            string substr = text.Substring(stIdx, idx - stIdx);

                            if (!float.TryParse(substr, out var value))
                                throw new Exception($"Invalid numerical literal: \"{substr}\"");

                            tokens.Add(new Token(TokenType.Number, value));
                            break;
                        }
                        else if (char.IsLetter(GetSafe()) || GetSafe() == '_')
                        {
                            tokens.Add(new Token(TokenType.Identifier, ReadIdentifier(withDot: false)));
                            break;
                        }

                        throw new Exception($"Unrecognized character: {GetSafe()}");

                }
            }

            tokens.Add(new Token(TokenType.EOS));

            return tokens;
        }
    }

    public class Parser
    {
        public Parser(string text)
        {
            tokens = Lexer.Tokenize(text);

            globalScope = new AcObject();
        }

        private AcObject globalScope;

        private List<Token> tokens;
        private int idx;

        private Token GetSafe(int offset = 0)
            => tokens[Math.Min(Math.Max(idx + offset, 0), tokens.Count)];

        private void Adv(int count = 1)
            => idx += count;

        private Token Require(TokenType t)
        {
            Token cTok = GetSafe();

            if (cTok.type != t)
                throw new Exception($"Unexpected token of type {cTok.type}, expected {t}");

            Adv();
            return cTok;
        }

        private object ParseVal()
        {
            var cTok = GetSafe();
            switch (cTok.type)
            {
                case TokenType.Identifier:
                case TokenType.String:
                case TokenType.Number:
                case TokenType.Color:
                    Adv();
                    return cTok.value;

                case TokenType.VariableRead:
                    Adv();
                    return globalScope.RequireNested(cTok.value as string);

                case TokenType.OpenList:
                    {
                        Adv();
                        AcList newList = new AcList();

                        bool done = false;
                        while (GetSafe().type != TokenType.CloseList && !done)
                        {
                            newList.Add(ParseVal());
                            switch (GetSafe().type)
                            {
                                case TokenType.EOS:
                                    throw new Exception("Unclosed array at end of file");
                                case TokenType.Comma:
                                    Adv();
                                    break;
                                default:
                                    done = true;
                                    break;
                            }
                        }

                        Require(TokenType.CloseList);
                        return newList;
                    }

                case TokenType.OpenObject:
                    {
                        Adv();
                        AcObject newObject = new AcObject();

                        var bases = new List<AcObject>();

                        bool done = false;
                        while (GetSafe().type != TokenType.CloseObject && !done)
                        {
                            //base objects
                            if (GetSafe().type == TokenType.Identifier &&
                               GetSafe().value as string == "from" &&
                               GetSafe(1).type != TokenType.Eq)
                            {
                                Adv();

                                var newBase = ParseVal();
                                if (newBase is not AcObject)
                                    throw new Exception("Base object must be an object");

                                bases.Add(newBase as AcObject);
                            }

                            //fields
                            else
                            {
                                var (k, v) = ParseAssign();
                                newObject.Add(k, v);
                            }

                            //end of statement
                            switch (GetSafe().type)
                            {
                                case TokenType.EOS:
                                    throw new Exception("Unclosed array at end of file");
                                case TokenType.Comma:
                                    Adv();
                                    break;
                                default:
                                    done = true;
                                    break;
                            }
                        }

                        Require(TokenType.CloseObject);

                        foreach (var baseObject in bases)
                        {
                            newObject.Merge(baseObject);
                        }

                        return newObject;
                    }

                default:
                    throw new Exception($"Unexcepted token of type {cTok.type}");
            }
        }

        private (string, object) ParseAssign()
        {
            var id = Require(TokenType.Identifier).value as string;
            Require(TokenType.Eq);
            var val = ParseVal();

            return (id, val);
        }

        public void ParseContents()
        {
            while (GetSafe().type != TokenType.EOS)
            {
                var (k, v) = ParseAssign();
                globalScope.Add(k, v);
            }

            Require(TokenType.EOS);
        }


        public static AcObject Parse(string text)
        {
            var parser = new Parser(text);
            parser.ParseContents();
            return parser.globalScope;
        }
    }
}
