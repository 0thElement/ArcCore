namespace ArcCore.Parsing
{
    public struct AffHold
    {
        public int timing;
        public int endTiming;
        public int track;
        public int timingGroup;

        public AffHold(int timing, int endTiming, int track, int timingGroup)
        {
            this.timing = timing;
            this.endTiming = endTiming;
            this.track = track;
            this.timingGroup = timingGroup;
        }
    }
}
