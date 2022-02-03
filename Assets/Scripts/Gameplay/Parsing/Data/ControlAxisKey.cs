using ArcCore.Storage;

namespace ArcCore.Gameplay.Parsing.Data
{
    public struct ControlAxisKey 
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = Settings.GetSpeedModifiedTime(value);
        }

        public float targetValue;
        public EasingType easing;
    }
}
