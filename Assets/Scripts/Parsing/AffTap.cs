﻿namespace ArcCore.Parsing.Aff
{
    public struct AffTap
    {
        private int _timing;
        public int Timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        public int track;
        public int timingGroup;
    }
}
