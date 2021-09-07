using System;
using System.Collections;
using System.Collections.Generic;

namespace ArcCore.Utilities
{
    public class IndexedList<T> : ICloneable, IEnumerable<T>
    {
        public static implicit operator IndexedList<T>(T[] arr) => new IndexedList<T>(arr);

        public int index;
        public IList<T> internalList;

        public void Reset()
        {
            index = 0;
        }

        public T Current => internalList[index];
        public T Next => internalList[index + 1];
        public T Previous => internalList[index - 1];

        public bool HasNext => index < internalList.Count - 1;
        public bool HasPrevious => index != 0;
        public bool Finished => index >= internalList.Count;
        public bool Unfinished => index < internalList.Count;

        public int Length => internalList.Count;

        public T this[int i]
        {
            get => internalList[i];
            set => internalList[i] = value;
        }

        public IndexedList(IList<T> list)
        {
            internalList = list;
            index = 0;
        }

        public IndexedList() : this(new List<T>())
        {}

        public object Clone() => new IndexedList<T>(internalList) { index = index };

        public IEnumerator<T> GetEnumerator() => internalList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)internalList).GetEnumerator();
    }
}