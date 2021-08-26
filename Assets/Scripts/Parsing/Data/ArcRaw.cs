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

        public float2 PositionAt(int time) => PositionAt(time, timing, endTiming, StartPos, EndPos, easing);
        public static float2 PositionAt(int time, int timing, int endTiming, float2 start, float2 end, ArcEasing easing)
            => new float2(
                    Conversion.GetXAt(
                        math.clamp(math.unlerp(time, timing, endTiming), 0, 1),
                        start.x, end.x, easing
                    ),
                    Conversion.GetYAt(
                        math.clamp(math.unlerp(time, timing, endTiming), 0, 1),
                        start.y, end.y, easing
                    )
               );
    }
}
