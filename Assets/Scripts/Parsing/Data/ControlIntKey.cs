namespace ArcCore.Parsing.Data
{
    public struct ControlIntKey
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public int value;
    }
}
