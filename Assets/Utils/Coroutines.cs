using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCoroutineUtils
{
    public static class Co
    {

//disable naming convention rules
#pragma warning disable IDE1006

        public static IEnumerator sequence(IEnumerator first, params IEnumerator[] others)
        {
            while(first.MoveNext())
                yield return first.Current;

            foreach(var coroutine in others)
                while(coroutine.MoveNext())
                    yield return coroutine.Current;
        }
        public static IEnumerator action(this Action action)
        {
            action();
            yield break;
        }
        public static IEnumerator procedure(Timer timer, Action<Timer> func)
        {
            while (!timer.IsComplete)
            {
                func(timer);
                yield return null;
            }
        }
        public static IEnumerator procedure(TimeSpan timeSpan, Action<Timer> func)
            => procedure(Timer.StartNew(timeSpan), func); 
        public static IEnumerator loop(Func<bool> condition, Action action, object yieldValue)
        {
            while (condition())
            {
                action();
                yield return yieldValue;
            }
        }
        public static IEnumerator iteration(IEnumerable iterator, Action<object> action, object yieldValue)
        {
            foreach (var i in iterator)
            {
                action(i);
                yield return yieldValue;
            }
        }
        public static IEnumerator iteration(Func<IEnumerable> iteratorGetter, Action<object> action, object yieldValue)
            => iteration(iteratorGetter(), action, yieldValue);
        public static IEnumerator iteration<T>(IEnumerable<T> iterator, Action<T> action, object yieldValue)
        {
            foreach (var i in iterator)
            {
                action(i);
                yield return yieldValue;
            }
        }
        public static IEnumerator iteration<T>(Func<IEnumerable<T>> iteratorGetter, Action<T> action, object yieldValue)
            => iteration<T>(iteratorGetter(), action, yieldValue);

        public static IEnumerator then(this IEnumerator coroutine, IEnumerator other)
            => sequence(coroutine, other); 
        public static IEnumerator only_yields(this IEnumerator coroutine, object yieldValue)
        {
            while (coroutine.MoveNext());
            yield return yieldValue;
        }
        public static IEnumerator also_yields(this IEnumerator coroutine, object yieldValue)
        {
            while (coroutine.MoveNext())
                yield return coroutine.Current;
            yield return yieldValue;
        }

        public static IEnumerator only_if(this IEnumerator coroutine, bool value)
        {
            if (value)
                while (coroutine.MoveNext())
                    yield return coroutine.Current;
            yield break;
        }
        public static IEnumerator only_if(this IEnumerator coroutine, Func<bool> func)
        {
            if (func())
                while (coroutine.MoveNext())
                    yield return coroutine.Current;
            yield break;
        }
        public static IEnumerator or_none(this IEnumerator coroutine)
            => only_if(coroutine, coroutine != null);

//reinstate naming convention
#pragma warning restore IDE1006

        public static Coroutine Start(this IEnumerator coroutine, MonoBehaviour parent)
            => parent.StartCoroutine(coroutine);
    }
}