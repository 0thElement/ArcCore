namespace ArcCore.Parsing.Aff
{
    public struct AffSceneControlEvent
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }
    }
}
