namespace ArcCore.Parsing.Aff
{
    public struct AffTiming
    {
        private int _timing;
        public int Timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        public float bpm;
        public float divisor;
    }
}
