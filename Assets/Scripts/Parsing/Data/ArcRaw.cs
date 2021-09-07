using Unity.Mathematics;
using ArcCore.Gameplay;

namespace ArcCore.Parsing.Data
{
    public struct ArcRaw
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        private int _endTiming;
        public int endTiming
        {
            get => _endTiming;
            set => _endTiming = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public float startX;
        public float endX;
        public ArcEasing easing;
        public float startY;
        public float endY;
        public int timingGroup;
        public int color;

        public float2 StartPos => new float2(startX, startY);
        public float2 EndPos => new float2(endX, endY);

        public float2 GetPosAt(int time)
            => Conversion.GetPosAt(math.unlerp(timing, endTiming, time), new float2(startX, startY), new float2(endX, endY), easing);
    }
}
