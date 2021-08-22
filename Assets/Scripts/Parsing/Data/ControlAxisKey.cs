namespace ArcCore.Parsing.Data
{
    public struct ControlAxisKey 
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public float targetValue;
        public EasingType easing;
    }
}
