namespace ArcCore.Parsing.Data
{
    public struct ControlStringKey
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = UserSettings.Instance.GetSpeedModifiedTime(value);
        }

        public string newValue;
    }
}
