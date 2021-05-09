namespace ArcCore.Parsing
{
    public struct TimingEvent
    {
        public int timing;
        public float floorPosition;
        public float bpm;

        public TimingEvent(int timing, float floorPosition, float bpm)
        {
            this.timing = timing;
            this.floorPosition = floorPosition;
            this.bpm = bpm;
        }
    }
}
