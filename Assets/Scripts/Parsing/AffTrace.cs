namespace ArcCore.Parsing.Aff
{
    public struct AffTrace
    {
        public int timing;
        public int endTiming;
        public float startX;
        public float endX;
        public ArcEasing easing;
        public float startY;
        public float endY;
        public int color;
        public int timingGroup;

        public AffTrace(int timing, int endTiming, float startX, float endX, ArcEasing easing, float startY, float endY, int color, int timingGroup)
        {
            this.timing = timing;
            this.endTiming = endTiming;
            this.startX = startX;
            this.endX = endX;
            this.easing = easing;
            this.startY = startY;
            this.endY = endY;
            this.color = color;
            this.timingGroup = timingGroup;
        }
    }
}
