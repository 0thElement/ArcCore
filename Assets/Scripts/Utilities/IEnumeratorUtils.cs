using System;
using System.Collections;
using System.Collections.Generic;

namespace ArcCore.Utilities
{
    public static class IEnumeratorUtils
    {
        public static IEnumerable<(T val, int index)> MatchByIndex<T>(this IEnumerable<T> enumerable)
        {
            int i = 0;
            foreach(var val in enumerable)
            {
                yield return (val, i);
                i++;
            }
        }
    }
}