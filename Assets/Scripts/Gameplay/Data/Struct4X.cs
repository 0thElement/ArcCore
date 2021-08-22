using System.Collections;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Data
{
    public struct Struct4X<T> : IEnumerable<T> where T : struct
    {
        private T val_0, val_1, val_2, val_3;

        public Struct4X(T value1, T value2, T value3, T value4)
        {
            val_0 = value1;
            val_1 = value2;
            val_2 = value3;
            val_3 = value4;
        }

        public T this[int idx]
        {
            get
            {
                //FIX LATER
                idx--;

                switch(idx)
                {
                    case 0: return val_0;
                    case 1: return val_1;
                    case 2: return val_2;
                    case 3: return val_3;
                    default: throw new System.IndexOutOfRangeException();
                }
            }
            set
            {
                //FIX LATER
                idx--;

                switch (idx)
                {
                    case 0:
                        val_0 = value;
                        break;
                    case 1:
                        val_1 = value;
                        break;
                    case 2:
                        val_2 = value;
                        break;
                    case 3:
                        val_3 = value;
                        break;
                    default:
                        throw new System.IndexOutOfRangeException();
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return val_0;
            yield return val_1;
            yield return val_2;
            yield return val_3;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}