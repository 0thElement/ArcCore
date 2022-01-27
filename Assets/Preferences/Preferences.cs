using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityPreferences
{
    public static class Preferences
    {
        private const string PreferencePrefix = "__prefs.";

        public delegate T Reader<T>(string name, T defaultValue = default);
        public delegate void Writer<T>(string name, T value);

        public delegate object Reader(string name, object defaultValue);
        public delegate void Writer(string name, object value);
        public delegate void Deleter(string name);

        public static Reader AsSimple<T>(this Reader<T> reader)
            => (n, o) => reader(n, (T)o);
        public static Reader<T> AsGeneric<T>(this Reader reader)
            => (n, o) => (T)reader(n, o);
        public static Writer AsSimple<T>(this Writer<T> writer)
            => (n, o) => writer(n, (T)o);
        public static Writer<T> AsGeneric<T>(this Writer writer)
            => (n, o) => writer(n, o);

        private static Dictionary<Type, Reader> customReaders = new Dictionary<Type, Reader>();
        private static Dictionary<Type, Writer> customWriters = new Dictionary<Type, Writer>();
        private static Dictionary<Type, Deleter> customDeleters = new Dictionary<Type, Deleter>();

        private static Dictionary<string, object> memoizedPrefs = new Dictionary<string, object>();

        public static void RegisterType<T>(Reader<T> reader, Writer<T> writer, Deleter deleter)
        {
            customReaders.Add(typeof(T), reader.AsSimple());
            customWriters.Add(typeof(T), writer.AsSimple());
            customDeleters.Add(typeof(T), n => deleter(n));
        }

        private static string AsName(this string str)
            => PreferencePrefix + str;

        public static int ReadInt(string name, int or = default)
            => PlayerPrefs.GetInt(name.AsName(), or);
        public static void WriteInt(string name, int val)
            => PlayerPrefs.SetInt(name.AsName(), val);
        public static float ReadFloat(string name, float or = default)
            => PlayerPrefs.GetFloat(name.AsName(), or);
        public static void WriteFloat(string name, float val)
            => PlayerPrefs.SetFloat(name.AsName(), val);
        public static string ReadString(string name, string or = default)
            => PlayerPrefs.GetString(name.AsName(), or);
        public static void WriteString(string name, string val)
            => PlayerPrefs.SetString(name.AsName(), val);
        public static bool ReadBool(string name, bool or = default)
            => PlayerPrefs.GetInt(name.AsName(), or ? 1 : 0) == 1;
        public static void WriteBool(string name, bool val)
            => PlayerPrefs.SetInt(name.AsName(), val ? 1 : 0);
        public static long ReadLong(string name, long or = default)
            => long.Parse(ReadString(name, or.ToString()));
        public static void WriteLong(string name, long value)
            => ReadString(name, value.ToString());
        public static ulong ReadULong(string name, ulong or = default)
            => ulong.Parse(ReadString(name, or.ToString()));
        public static void WriteULong(string name, ulong value)
            => ReadString(name, value.ToString());
        private static void DeleteSimple(string name)
            => PlayerPrefs.DeleteKey(name.AsName());

        public static Color32 ReadColor(string name, Color32 or = default)
            => new Color32(
                (byte)ReadInt(name + ".r", or.r),
                (byte)ReadInt(name + ".g", or.g),
                (byte)ReadInt(name + ".b", or.b),
                (byte)ReadInt(name + ".a", or.a)
            );
        public static void WriteColor(string name, Color32 value)
        {
            ReadInt(name + ".r", value.r);
            ReadInt(name + ".g", value.g);
            ReadInt(name + ".b", value.b);
            ReadInt(name + ".a", value.a);
        }
        private static void DeleteColor(string name)
        {
            DeleteSimple(name + ".r");
            DeleteSimple(name + ".g");
            DeleteSimple(name + ".b");
            DeleteSimple(name + ".a");
        }

        public static T? ReadNullable<T>(string name, T? or = null) where T : struct
            => ReadNullable(typeof(T), name, or) as T?;
        public static object ReadNullable(Type inner, string name, object or)
            => ReadBool(name + ".exists", false) ? ReaderForType(inner)(name, or) : null;
        public static void WriteNullable<T>(string name, T? value) where T: struct
            => WriteNullable(typeof(T), name, value);
        public static void WriteNullable(Type inner, string name, object value)
        {
            var hasValueProp = typeof(Nullable<>).MakeGenericType(inner).GetProperty("HasValue");
            var valueProp = typeof(Nullable<>).MakeGenericType(inner).GetProperty("Value");

            var hasValue = (bool)hasValueProp.GetValue(value);

            WriteBool(name + ".exists", hasValue);
            if (hasValue)
            {
                WriterForType(inner)(name, valueProp.GetValue(value));
            }
        }
        public static void DeleteNullable<T>(string name)
            => DeleteNullable(typeof(T), name);
        public static void DeleteNullable(Type inner, string name)
        {
            DeleteSimple(name + ".exists");
            DeleterForType(inner)(name);
        }

        public static T ReadEnum<T>(string name, T or = default) where T : struct, Enum
            => (T)ReadEnum(typeof(T), name, or);
        public static object ReadEnum(Type t, string name, object or)
            => Enum.Parse(t, ReadString(name));
        public static void WriteEnum<T>(string name, T value) where T : struct, Enum
            => WriteEnum(typeof(T), name, value);
        public static void WriteEnum(Type t, string name, object value)
            => WriteString(name, value.ToString());

        private static Array ReadEnumerable(Type inner, string name, int count)
        {
            var arr = Array.CreateInstance(inner, count);

            for (int i = 0; i < count; i++)
            {
                arr.SetValue(ReaderForType(inner)(name + "." + i, default), i);
            }

            return arr;
        }
        private static int Count(this IEnumerable enumerable)
        {
            int i = 0;
            foreach(var _ in enumerable)
            {
                i++;
            }
            return i;
        }
        private static void WriteEnumerable(Type inner, string name, IEnumerable value, int originalLength)
        {
            var origLen = ReadInt(name + ".len", InvalidLen);

            if (value != null)
            {
                int i = 0;
                foreach(var item in value)
                {
                    WriterForType(inner)(name + "." + i, item);
                    i++;
                }
            }

            if (origLen != InvalidLen)
            {
                for (int i = value?.Count() ?? 0 - 1; i >= origLen; i--)
                {
                    PlayerPrefs.DeleteKey(name + "." + i);
                }
            }
        }
        private static void DeleteEnumerable(Type inner, string name, int len)
        {
            for (int i = 0; i < len; i++)
            {
                DeleterForType(inner)(name + "." + i);
            }
        }

        private const int InvalidLen = -2;
        private const int NullLen = -1;
        public static T[] ReadArray<T>(string name, T[] or = default)
            => (T[])ReadArray(typeof(T), name, or);
        public static object ReadArray(Type inner, string name, object or)
        {
            var count = ReadInt(name + ".len", InvalidLen);

            if (count == InvalidLen)
                return or;
            if (count == NullLen)
                return null;

            return ReadEnumerable(inner, name, count);
        }
        public static void WriteArray<T>(string name, T[] value) 
            => WriteArray(typeof(T), name, value);
        public static void WriteArray(Type inner, string name, object value)
        {
            var origLen = ReadInt(name + ".len", InvalidLen);
            Array arr = (Array)value;

            if (value == null)
            {
                WriteInt(name + ".len", NullLen);
            }
            else
            {
                WriteInt(name + ".len", arr.Length);
            }

            WriteEnumerable(inner, name, arr, origLen);
        }
        public static void DeleteArray<T>(string name)
            => DeleteArray(typeof(T), name);
        public static void DeleteArray(Type inner, string name)
        {
            var len = ReadInt(name + ".len", InvalidLen);
            DeleteSimple(name + ".len");
            DeleteEnumerable(inner, name, len);
        }

        public static Dictionary<K, V> ReadDictionary<K, V>(string name, Dictionary<K, V> or = default)
            => (Dictionary<K,V>)ReadDictionary(typeof(K), typeof(V), name, or);
        public static object ReadDictionary(Type key, Type value, string name, object or)
        {
            IDictionary val = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(key, value));

            ReadInt(name + ".len", val.Count);

            IList keys = ReadEnumerable(key, name + ".keys", val.Count);
            IList values = ReadEnumerable(value, name + ".values", val.Count);

            for(int i = 0; i < keys.Count; i++)
            {
                val.Add(keys[i], values[i]);
            }

            return val;
        }
        public static void WriteDictionary<K, V>(string name, Dictionary<K, V> value)
            => WriteDictionary(typeof(K), typeof(V), name, value);
        public static void WriteDictionary(Type key, Type valueT, string name, object value)
        {
            var valueD = (IDictionary)value;

            WriteInt(name + ".len", valueD.Count);

            WriteEnumerable(key, name + ".keys", valueD.Keys, valueD.Count);
            WriteEnumerable(valueT, name + ".values", valueD.Values, valueD.Count);
        }
        public static void DeleteDictionary(Type key, Type value, string name)
        {
            var len = ReadInt(name + ".len", InvalidLen);
            DeleteSimple(name + ".len");
            DeleteEnumerable(key, name + ".keys", len);
            DeleteEnumerable(value, name + ".values", len);
        }

        public static T ReadCustom<T>(string name, T or = default)
            => (T)ReadCustom(typeof(T), name, or);
        public static object ReadCustom(Type t, string name, object or)
        {
            if (customReaders.ContainsKey(t))
                return customReaders[t](name, or);

            throw new NotImplementedException($"The type '{t}' is not a valid type for preferences.");
        }
        public static void WriteCustom<T>(string name, T value)
            => WriteCustom(typeof(T), name, value);
        public static void WriteCustom(Type t, string name, object value)
        {
            if (customWriters.ContainsKey(t))
            {
                customWriters[t](name, value);
                return;
            }

            throw new NotImplementedException($"The type '{t}' is not a valid type for preferences.");
        }
        public static void DeleteCustom<T>(string name)
            => DeleteCustom(typeof(T), name);
        public static void DeleteCustom(Type t, string name)
        {
            if (customDeleters.ContainsKey(t))
            {
                customDeleters[t](name);
                return;
            }

            throw new NotImplementedException($"The type '{t}' is not a valid type for preferences.");
        }

        private static T Pun<T>(this object val)
            => (T)val;
        private static Reader ReaderForType(Type t)
        {
            if (t == typeof(int))
                return (n, o) => ReadInt(n, o.Pun<int>());
            if (t == typeof(float))
                return (n, o) => ReadFloat(n, o.Pun<float>());
            if (t == typeof(string))
                return (n, o) => ReadString(n, o.Pun<string>());
            if (t == typeof(bool))
                return (n, o) => ReadBool(n, o.Pun<bool>());
            if (t == typeof(long))
                return (n, o) => ReadLong(n, o.Pun<long>());
            if (t == typeof(ulong))
                return (n, o) => ReadULong(n, o.Pun<ulong>());
            if (t == typeof(Color32) || t == typeof(Color))
                return (n, o) => ReadColor(n, o.Pun<Color32>());
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                return (n, o) => ReadNullable(t.GenericTypeArguments[0], n, o);
            if (t.IsEnum)
                return (n, o) => ReadEnum(t, n, o);
            if (t.IsArray)
                return (n, o) => ReadArray(t.GetElementType(), n, o);
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return (n, o) => ReadDictionary(t.GenericTypeArguments[0], t.GenericTypeArguments[1], n, o);

            return (n, o) => ReadCustom(t, n, o);
        }
        private static Writer WriterForType(Type t)
        {
            if (t == typeof(int))
                return (n, o) => WriteInt(n, o.Pun<int>());
            if (t == typeof(float))
                return (n, o) => WriteFloat(n, o.Pun<float>());
            if (t == typeof(string))
                return (n, o) => WriteString(n, o.Pun<string>());
            if (t == typeof(bool))
                return (n, o) => WriteBool(n, o.Pun<bool>());
            if (t == typeof(long))
                return (n, o) => WriteLong(n, o.Pun<long>());
            if (t == typeof(ulong))
                return (n, o) => WriteULong(n, o.Pun<ulong>());
            if (t == typeof(Color32) || t == typeof(Color))
                return (n, o) => WriteColor(n, o.Pun<Color32>());
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                return (n, o) => WriteNullable(t.GenericTypeArguments[0], n, o);
            if (t.IsEnum)
                return (n, o) => WriteEnum(t, n, o);
            if (t.IsArray)
                return (n, o) => WriteArray(t.GetElementType(), n, o);
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return (n, o) => WriteDictionary(t.GenericTypeArguments[0], t.GenericTypeArguments[1], n, o);

            return (n, o) => ReadCustom(t, n, o);
        }
        private static Deleter DeleterForType(Type t)
        {
            if (t == typeof(int) || t == typeof(long) || t == typeof(ulong) || t == typeof(float) || t == typeof(string) || t.IsEnum)
                return DeleteSimple;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                return n => DeleteNullable(t.GenericTypeArguments[0], n);
            if (t == typeof(Color32) || t == typeof(Color))
                return n => DeleteColor(n);
            if (t.IsArray)
                return n => DeleteArray(t.GetElementType(), n);
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return n => DeleteDictionary(t.GenericTypeArguments[0], t.GenericTypeArguments[1], n);

            return n => DeleteCustom(t, n);
        }

        public static bool HasPref(string name)
            => PlayerPrefs.HasKey(name.AsName());

        public static T Read<T>(string name, T or = default)
            => (T)ReaderForType(typeof(T))(name, or);
        public static void Write<T>(string name, T value)
            => WriterForType(typeof(T))(name, value);
        public static void Delete<T>(string name)
            => DeleterForType(typeof(T))(name);

        public static T Get<T>(string name, T or = default)
        {
            if(!memoizedPrefs.ContainsKey(name))
            {
                memoizedPrefs.Add(name, Read<T>(name, or));
            }

            return (T)memoizedPrefs[name];
        }
        public static void Set<T>(string name, T value)
        {
            memoizedPrefs[name] = value;
            Write<T>(name, value);
        }
    }
}
