namespace ArcCore.Parsing.Data
{
    public struct HoldRaw
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = UserSettings.Instance.GetSpeedModifiedTime(value);
        }

        private int _endTiming;
        public int endTiming
        {
            get => _endTiming;
            set => _endTiming = UserSettings.Instance.GetSpeedModifiedTime(value);
        }

        public int track;
        public int timingGroup;
    }
}
