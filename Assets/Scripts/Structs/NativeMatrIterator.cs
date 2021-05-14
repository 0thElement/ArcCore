using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;

namespace ArcCore.Structs
{
    public unsafe struct NativeRefArray<T> : IDisposable where T : unmanaged
    {
        private NativeArray<IntPtr> values;

        public NativeRefArray(NativeArray<T> svalues, Allocator allocator)
        {
            values = new NativeArray<IntPtr>(svalues.Length, allocator);
            for(int i = 0; i < svalues.Length; i++)
            {
                T val = svalues[i];
                values[i] = (IntPtr)(&val);
            }
        }
        public NativeRefArray(T[] svalues, Allocator allocator)
        {
            values = new NativeArray<IntPtr>(svalues.Length, allocator);
            for (int i = 0; i < svalues.Length; i++)
            {
                T val = svalues[i];
                values[i] = (IntPtr)(&val);
            }
        }
        public NativeRefArray(int length, Allocator allocator, NativeArrayOptions options)
        {
            values = new NativeArray<IntPtr>(length, allocator, options);
            for (int i = 0; i < length; i++)
            {
                T val = default;
                values[i] = (IntPtr)(&val);
            }
        }

        public T* this[int index]
        {
            get => (T*)values[index];
            set => values[index] = (IntPtr)value;
        }

        public int Length => values.Length;
        public bool IsCreated => values.IsCreated;

        public void Dispose()
        {
            values.Dispose();
        }
    }

    [Obsolete(null, error: true)]
    public struct NativeMatrIterator<T> : IDisposable where T : struct
    {
        private NativeArray<T> contents;
        private readonly NativeArray<int> startIndices;
        private NativeArray<int> indices;

        public NativeArray<int> Indices => indices;
        public int RowCount => startIndices.Length;

        public NativeMatrIterator(T[][] matr, Allocator allocator)
        {
            indices = new NativeArray<int>(matr.Length, allocator);
            startIndices = new NativeArray<int>(matr.Length + 1, allocator);
            int midx = 0, r;

            for (r = 1; r < matr.Length; r++)
            {
                midx += matr[r - 1].Length;
                startIndices[r] = midx;
            }

            //TEST
            if (r != matr.Length - 1) throw new Exception("FUCK");
            //ENDTEST

            contents = new NativeArray<T>(midx + matr[r].Length, allocator);

            for (int i = 0; i < matr.Length; i++)
            {
                for (int j = 0; j < matr[i].Length; j++)
                {
                    contents[i + startIndices[i] + j] = matr[i][j];
                }
            }
        }

        public T Current(int row) => this[row, indices[row]];
        public T SetCurrent(int row, T value) => this[row, indices[row]] = value;
        public bool HasCurrent(int row) => indices[row] >= startIndices[row + 1];

        public T this[int r, int c]
        {
            get => contents[r + startIndices[r] + c];
            set => contents[r + startIndices[r] + c] = value;
        }

        public bool MoveNext(int row) => ++indices[row] < startIndices[row];

        public void Reset(int row) => indices[row] = 0;

        public void Dispose()
        {
            contents.Dispose();
            startIndices.Dispose();
            indices.Dispose();

            GC.SuppressFinalize(this);
        }

        public bool HasNext(int row) => indices[row] < startIndices[row + 1];
        public bool HasPrev(int row) => indices[row] > startIndices[row];

        public T PeekAhead(int row, int by) => this[row, indices[row] + by];
    }
}