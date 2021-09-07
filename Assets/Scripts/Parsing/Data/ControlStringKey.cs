namespace ArcCore.Parsing.Data
{
    public struct ControlStringKey
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public string newValue;
    }
}
