using System;
using System.Collections;
using System.Collections.Generic;

namespace ArcCore.Utilities
{
    public class IndexedArray<T> : ICloneable, IEnumerable<T>, IList<T>
    {
        public static implicit operator IndexedArray<T>(T[] arr) => new IndexedArray<T>(arr);

        public int index;
        public T[] array;

        public void Reset()
        {
            index = 0;
        }

        public T Current => array[index];
        public T Next => array[index + 1];
        public T Previous => array[index - 1];

        public bool HasNext => index < array.Length - 1;
        public bool HasPrevious => index != 0;
        public bool Finished => index >= array.Length;
        public bool Unfinished => index < array.Length;

        public int Length => array.Length;
        int ICollection<T>.Count => ((ICollection<T>)array).Count;
        public bool IsReadOnly => ((ICollection<T>)array).IsReadOnly;

        public T this[int i]
        {
            get => array[i];
            set => array[i] = value;
        }

        public IndexedArray(T[] array)
        {
            this.array = array;
            index = 0;
        }

        public object Clone() => new IndexedArray<T>(array) { index = index };
        IEnumerator IEnumerable.GetEnumerator() => array.GetEnumerator();
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)array).GetEnumerator();

        public int IndexOf(T item)
        {
            return ((IList<T>)array).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)array).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<T>)array).RemoveAt(index);
        }

        public void Add(T item)
        {
            ((ICollection<T>)array).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)array).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)array).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)this.array).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)array).Remove(item);
        }
    }
}