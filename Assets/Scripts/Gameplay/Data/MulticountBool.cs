namespace ArcCore.Gameplay.Data
{
    public readonly struct MulticountBool
    {
        private readonly int counter;
        private MulticountBool(int c) => counter = c;

        public bool BoolValue => (counter != 0);

        public static implicit operator int(MulticountBool a) => a.counter;
        public static MulticountBool operator +(MulticountBool a, int b) => new MulticountBool(a.counter + b);
        public static MulticountBool operator -(MulticountBool a, int b) => new MulticountBool(a.counter - b);
        public static MulticountBool operator ++(MulticountBool a) => a + 1;
        public static MulticountBool operator --(MulticountBool a) => a - 1;

        public static bool operator &(MulticountBool a, MulticountBool b) => a.BoolValue && b.BoolValue;
        public static bool operator |(MulticountBool a, MulticountBool b) => a.BoolValue || b.BoolValue;
        public static bool operator !(MulticountBool a) => !a.BoolValue;

        public static bool operator true(MulticountBool a) => a.BoolValue;
        public static bool operator false(MulticountBool a) => !a.BoolValue;
    }
}