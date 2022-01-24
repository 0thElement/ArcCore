using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityPreferences
{
    public static class Preferences
    {
        private const string PreferencePrefix = "__prefs.";

        private static string AsName(this string str) 
            => PreferencePrefix + str;

        public static int GetInt(string name, int or = default)
            => PlayerPrefs.GetInt(name.AsName(), or);
        public static void SetInt(string name, int val)
            => PlayerPrefs.SetInt(name.AsName(), val);
        public static float GetFloat(string name, float or = default)
            => PlayerPrefs.GetFloat(name.AsName(), or);
        public static void SetFloat(string name, float val)
            => PlayerPrefs.SetFloat(name.AsName(), val);
        public static string GetString(string name, string or = default)
            => PlayerPrefs.GetString(name.AsName(), or);
        public static void SetString(string name, string val)
            => PlayerPrefs.SetString(name.AsName(), val);
        public static bool GetBool(string name, bool or = default)
            => PlayerPrefs.GetInt(name.AsName(), or ? 1 : 0) == 1;
        public static void SetBool(string name, bool val)
            => PlayerPrefs.SetInt(name.AsName(), val ? 1 : 0);
        public static long GetLong(string name, long or = default)
            => long.Parse(GetString(name, or.ToString()));
        public static void SetLong(string name, long value)
            => SetString(name, value.ToString());
        public static ulong GetULong(string name, ulong or = default)
            => ulong.Parse(GetString(name, or.ToString()));
        public static void SetULong(string name, ulong value)
            => SetString(name, value.ToString());
        public static Color32 GetColor(string name, Color32 or = default)
            => new Color32(
                (byte)GetInt(name + ".r", or.r),
                (byte)GetInt(name + ".g", or.g),
                (byte)GetInt(name + ".b", or.b), 
                (byte)GetInt(name+ ".a", or.a)
            );
        public static void SetColor(string name, Color32 value)
        {
            SetInt(name + ".r", value.r);
            SetInt(name + ".g", value.g);
            SetInt(name + ".b", value.b);
            SetInt(name + ".a", value.a);
        }
        public static T GetEnum<T>(string name, T or = default) where T : struct, Enum
        {
            Enum.TryParse(GetString(name, or.ToString()), out T result);
            return result;
        }
        private static object GetEnum(Type enumT, string name, object or = default)
        {
            return typeof(Preferences).GetMethod(nameof(GetEnum), BindingFlags.Public).MakeGenericMethod(enumT).Invoke(null, new object[] { name, or });
        }
        public static void SetEnum<T>(string name, T value) where T : struct, Enum
            => SetString(name, value.ToString());
        private static void SetEnum(Type enumT, string name, object value)
        {
            typeof(Preferences).GetMethod(nameof(SetEnum), BindingFlags.Public).MakeGenericMethod(enumT).Invoke(null, new object[] { name, value });
        }
        private const int InvalidLen = -2;
        private const int NullLen = -1;
        public static T[] GetArray<T>(string name, T[] or = default)
        {
            var count = GetInt(name + ".len", InvalidLen);

            if (count == InvalidLen)
                return or;
            if (count == NullLen)
                return null;

            T[] arr = new T[count];

            for (int i = 0; i < count; i++)
            {
                arr[i] = GetterForType<T>()(name + "." + i, default);
            }

            return arr;
        }
        private static object GetArray(Type arrayT, string name, object or = default)
        {
            return typeof(Preferences).GetMethod(nameof(GetArray), BindingFlags.Public).MakeGenericMethod(arrayT).Invoke(null, new object[] { name, or });
        }
        public static void SetArray<T>(string name, T[] value)
        {
            var origLen = GetInt(name + ".len", InvalidLen);

            if(value == null)
            {
                SetInt(name + ".len", NullLen);
            }
            else
            {
                SetInt(name + ".len", value.Length);
                for(int i = 0; i < value.Length; i++)
                {
                    SetterForType<T>()(name + "." + i, value[i]);
                }
            }

            if (origLen != InvalidLen)
            {
                var len = value?.Length ?? 0;
                for (int i = len - 1; i >= origLen; i--)
                {
                    PlayerPrefs.DeleteKey(name + "." + i);
                }
            }
        }
        private static object SetArray(Type arrayT, string name, object value)
        {
            return typeof(Preferences).GetMethod(nameof(SetArray), BindingFlags.Public).MakeGenericMethod(arrayT).Invoke(null, new object[] { name, value });
        }

        private static T Pun<T>(this object val)
            => (T)val;
        private static Func<string, T, T> GetterForType<T>()
        {
            var t = typeof(T);

            if (t == typeof(int))
                return (n, o) => GetInt(n, o.Pun<int>()).Pun<T>();
            if (t == typeof(float))
                return (n, o) => GetFloat(n, o.Pun<float>()).Pun<T>();
            if (t == typeof(string))
                return (n, o) => GetString(n, o.Pun<string>()).Pun<T>();
            if (t == typeof(bool))
                return (n, o) => GetBool(n, o.Pun<bool>()).Pun<T>();
            if (t == typeof(long))
                return (n, o) => GetLong(n, o.Pun<long>()).Pun<T>();
            if (t == typeof(ulong))
                return (n, o) => GetULong(n, o.Pun<ulong>()).Pun<T>();
            if (t == typeof(Color32))
                return (n, o) => GetColor(n, o.Pun<Color32>()).Pun<T>();
            if (t.IsEnum)
                return (n, o) => GetEnum(t, n, o).Pun<T>();
            if (t.IsArray)
                return (n, o) => GetArray(t, n, o).Pun<T>();

            throw new NotImplementedException($"The type '{t}' is not a valid type for preferences.");
        }
        private static Action<string, T> SetterForType<T>()
        {
            var t = typeof(T);

            if (t == typeof(int))
                return (n, o) => SetInt(n, o.Pun<int>());
            if (t == typeof(float))
                return (n, o) => SetFloat(n, o.Pun<float>());
            if (t == typeof(string))
                return (n, o) => SetString(n, o.Pun<string>());
            if (t == typeof(bool))
                return (n, o) => SetBool(n, o.Pun<bool>());
            if (t == typeof(long))
                return (n, o) => SetLong(n, o.Pun<long>());
            if (t == typeof(ulong))
                return (n, o) => SetULong(n, o.Pun<ulong>());
            if (t == typeof(Color32))
                return (n, o) => SetColor(n, o.Pun<Color32>());
            if (t.IsEnum)
                return (n, o) => SetEnum(t, n, o);
            if (t.IsArray)
                return (n, o) => SetArray(t, n, o);

            throw new NotImplementedException($"The type '{t}' is not a valid type for preferences.");
        }

        public static bool HasPref(string name)
            => PlayerPrefs.HasKey(name.AsName());
        public static void DeletePref(string name)
            => PlayerPrefs.DeleteKey(name.AsName());
    }
}
