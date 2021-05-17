using System;

namespace ArcCore.Structs
{
    [Obsolete(null, true)]
    public readonly unsafe struct DRef<T> where T: unmanaged
    {
        private readonly T** ptr;

        private DRef(T* ptr)
        {
            this.ptr = &ptr;
        }

        public T? Val
        {
            get => (ptr == (T**)IntPtr.Zero) ? (T?)null : (T?)**ptr;
            set
            {
                if(value is null)
                {
                    *ptr = (T*)IntPtr.Zero;
                } 
                else
                {
                    **ptr = value.Value;
                }
            }
        }

        public ref T ValUnsafe
        {
            get
            {
                if (ptr == (T**)IntPtr.Zero)
                {
                    throw new Exception();
                }
                return ref **ptr;
            }
        }

        public static DRef<T> New() => 
            new DRef<T>((T*)IntPtr.Zero);
        public static DRef<T> New(T value) =>
            new DRef<T>(&value);
    }
}