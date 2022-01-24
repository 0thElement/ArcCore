namespace ArcCore.Parsing.Data
{
    public struct ChartSceneControlEvent
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = Settings.GetSpeedModifiedTime(value);
        }
    }
}
