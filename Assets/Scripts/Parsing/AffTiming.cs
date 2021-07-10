namespace ArcCore.Parsing.Aff
{
    public struct AffTiming
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public float bpm;
        public float divisor;
    }
}
