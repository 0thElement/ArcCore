namespace ArcCore.Parsing.Data
{
    public struct TapRaw
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = UserSettings.Instance.GetSpeedModifiedTime(value);
        }

        public int track;
        public int timingGroup;
    }
}
