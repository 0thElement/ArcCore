namespace ArcCore.Parsing.Aff
{
    public struct AffHold
    {
        private int _timing;
        public int Timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        private int _endTiming;
        public int EndTiming
        {
            get => _endTiming;
            set => _endTiming = GameSettings.GetSpeedModifiedTime(value);
        }

        public int track;
        public int timingGroup;
    }
}
