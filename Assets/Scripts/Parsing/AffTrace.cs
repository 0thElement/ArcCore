﻿namespace ArcCore.Parsing.Aff
{
    public struct AffTrace
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        private int _endTiming;
        public int endTiming
        {
            get => _endTiming;
            set => _endTiming = GameSettings.GetSpeedModifiedTime(value);
        }

        public float startX;
        public float endX;
        public ArcEasing easing;
        public float startY;
        public float endY;
        public int color;
        public int timingGroup;
    }
}
