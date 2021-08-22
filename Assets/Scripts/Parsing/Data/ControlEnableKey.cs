namespace ArcCore.Parsing.Data
{
    public struct ControlEnableKey
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public bool newValue;
    }
}
