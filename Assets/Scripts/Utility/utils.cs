using ArcCore.MonoBehaviours;
using ArcCore.MonoBehaviours.EntityCreation;
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Utility
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Intrinsic-esque")]
    public static class utils
    {
        public static T[] new_fill<T>(int length, T value = default) where T: struct
        {
            T[] newarr = new T[length];
            for(int i = 0; i < length; i++)
            {
                newarr[i] = value;
            }
            return newarr;
        }

        public static T[] new_fill_aclen<T>(T value = default) where T : struct
            => new_fill<T>(ArcEntityCreator.ColorCount, value);
    }
}