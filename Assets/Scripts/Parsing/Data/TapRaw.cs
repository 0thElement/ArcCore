namespace ArcCore.Parsing.Data
{
    public struct TapRaw
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = Settings.GetSpeedModifiedTime(value);
        }

        public int track;
        public int timingGroup;
    }
}
