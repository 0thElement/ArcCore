using ArcCore.MonoBehaviours;
using ArcCore.MonoBehaviours.EntityCreation;
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
    }
}