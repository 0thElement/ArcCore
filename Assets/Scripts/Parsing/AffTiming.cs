namespace ArcCore.Parsing.Aff
{
    public struct AffTiming
    {
        public int timing;
        public float bpm;
        public float divisor;

        public AffTiming(int timing, float bpm, float divisor)
        {
            this.timing = timing;
            this.bpm = bpm;
            this.divisor = divisor;
        }
    }
}
