namespace ArcCore.Parsing.Data
{
    public struct HoldRaw
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = Settings.GetSpeedModifiedTime(value);
        }

        private int _endTiming;
        public int endTiming
        {
            get => _endTiming;
            set => _endTiming = Settings.GetSpeedModifiedTime(value);
        }

        public int track;
        public int timingGroup;
    }
}
