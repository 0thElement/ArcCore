namespace ArcCore.Structs
{
    public readonly unsafe struct Ref<T> where T: unmanaged
    {
        private readonly T* ptr;

        private Ref(T* ptr)
        {
            this.ptr = ptr;
        }

        public ref T Val => ref *ptr;

        public static Ref<T> New(T value = default) =>
            new Ref<T>(&value);
    }
}