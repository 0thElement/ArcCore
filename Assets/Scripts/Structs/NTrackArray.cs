using System.Collections;
using System.Collections.Generic;

namespace ArcCore.Structs
{
    public struct NTrackArray<T> : IEnumerable<T> where T : struct
    {
        private T v1, v2, v3, v4;

        public NTrackArray(T value1, T value2, T value3, T value4)
        {
            v1 = value1;
            v2 = value2;
            v3 = value3;
            v4 = value4;
        }

        public T this[int idx]
        {
            get
            {
                switch(idx)
                {
                    case 1: return v1;
                    case 2: return v2;
                    case 3: return v3;
                    case 4: return v4;
                    default: throw new System.IndexOutOfRangeException();
                }
            }
            set
            {
                switch (idx)
                {
                    case 1:
                        v1 = value;
                        break;
                    case 2:
                        v2 = value;
                        break;
                    case 3:
                        v3 = value;
                        break;
                    case 4:
                        v4 = value;
                        break;
                    default:
                        throw new System.IndexOutOfRangeException();
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private int n;
            private readonly NTrackArray<T> v;

            internal Enumerator(NTrackArray<T> arr)
            {
                v = arr;
                n = 0;
            }

            public T Current => v[n];
            object IEnumerator.Current => Current;

            public void Dispose()
            {}

            public bool MoveNext()
            {
                return ++n <= 4;
            }

            public void Reset()
            {
                n = 0;
            }
        }
    }
}