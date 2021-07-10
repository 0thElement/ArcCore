namespace ArcCore.Parsing.Aff
{
    public struct AffHold
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        private int _endTiming;
        public int endTiming
        {
            get => _endTiming;
            set => _endTiming = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public int track;
        public int timingGroup;
    }
}
