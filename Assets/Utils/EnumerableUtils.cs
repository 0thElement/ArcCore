using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.Utitlities
{
    public static class EnumerableUtils
    {
        public static IEnumerable<R> SelectMultiple<T, R>(this IEnumerable<T> ienum, params Func<T, R>[] funcs)
            => ienum.SelectMany(v => funcs.Select(f => f(v)));
    }
}