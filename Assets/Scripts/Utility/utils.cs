using ArcCore.Behaviours;
using ArcCore.Behaviours.EntityCreation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Utility
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Intrinsic-esque")]
    public static class utils
    {
        public static T[] newarr_fill<T>(int length, T value = default) where T: struct
        {
            T[] newarr = new T[length];
            for(int i = 0; i < length; i++)
            {
                newarr[i] = value;
            }
            return newarr;
        }

        public static T[] newarr_fill_aclen<T>(T value = default) where T : struct
            => newarr_fill<T>(ArcEntityCreator.ColorCount, value);

        public static T[] flatten<T>(IEnumerable<object> enumerable) 
        {
            List<T> list = new List<T>();
            foreach(object obj in enumerable)
            {
                if (obj is T value) list.Add(value);
                if (obj is IEnumerable<object> values) list.AddRange(flatten<T>(values));
                throw new ArgumentException("INVALID TYPE FOR ARG");
            }
            return list.ToArray();
        }

        public static T[][] list_arr_2d<T>(List<List<T>> list)
        {
            T[][] n = new T[list.Count][];
            for(int i = 0; i < n.Length; i++)
            {
                n[i] = list[i].ToArray();
            }
            return n;
        }

        public static int b2i(bool b)
            => b ? 1 : 0;

        public static bool i2b(int i)
            => i != 0;
    }
}