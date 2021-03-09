using ArcCore.MonoBehaviours;
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Utility
{
    public static unsafe class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* New<T>(T def = default) where T : unmanaged
            => &def;
    }
}