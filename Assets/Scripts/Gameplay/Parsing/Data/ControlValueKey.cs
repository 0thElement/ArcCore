using ArcCore.Storage;

namespace ArcCore.Gameplay.Parsing.Data
{
    public struct ControlValueKey<T>
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = Settings.GetSpeedModifiedTime(value);
        }

        public T value;
    }
}
