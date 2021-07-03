namespace ArcCore.Parsing.Aff
{
    public struct TimingEvent
    {
        public int timing;
        public float baseFloorPosition;
        public float bpm;

        public TimingEvent(int timing, float floorPosition, float bpm)
        {
            this.timing = timing;
            this.baseFloorPosition = floorPosition;
            this.bpm = bpm;
        }
    }
}
