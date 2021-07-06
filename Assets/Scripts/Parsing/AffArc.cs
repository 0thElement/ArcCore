using ArcCore.Math;
using ArcCore.Gameplay.Utility;
using Unity.Mathematics;
using ArcCore.Gameplay;

namespace ArcCore.Parsing.Aff
{
    public struct AffArc
    {
        private int _timing;
        public int Timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        private int _endTiming;
        public int EndTiming
        {
            get => _endTiming;
            set => _endTiming = GameSettings.GetSpeedModifiedTime(value);
        }

        public float startX;
        public float endX;
        public ArcEasing easing;
        public float startY;
        public float endY;
        public int timingGroup;

        public float2 StartPos => new float2(startX, startY);
        public float2 EndPos => new float2(endX, endY);

        public float2 PositionAt(int time) => PositionAt(time, Timing, EndTiming, StartPos, EndPos, easing);
        public Circle2D ColliderAt(int time) => ColliderAt(time, Timing, EndTiming, StartPos, EndPos, easing);

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

        public static Circle2D ColliderAt(int time, int timing, int endTiming, float2 start, float2 end, ArcEasing easing)
            => new Circle2D(PositionAt(time, timing, endTiming, start, end, easing), Constants.ArcColliderRadius);
    }
}
