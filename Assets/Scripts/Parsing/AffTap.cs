namespace ArcCore.Parsing.Aff
{
    public struct AffTap
    {
        public int timing;
        public int track;
        public int timingGroup;

        public AffTap(int timing, int track, int timingGroup)
        {
            this.timing = timing;
            this.track = track;
            this.timingGroup = timingGroup;
        }
    }
}
