using ArcCore.Math;
using ArcCore.Gameplay.Utility;
using Unity.Mathematics;
using ArcCore.Gameplay;

namespace ArcCore.Parsing.Aff
{
    public struct AffArc
    {
        public int timing;
        public int endTiming;
        public float startX;
        public float endX;
        public ArcEasing easing;
        public float startY;
        public float endY;
        public int timingGroup;

        public float2 StartPos => new float2(startX, startY);
        public float2 EndPos => new float2(endX, endY);

        public AffArc(int timing, int endTiming, float startX, float endX, ArcEasing easing, float startY, float endY, int timingGroup)
        {
            this.timing = timing;
            this.endTiming = endTiming;
            this.startX = startX;
            this.endX = endX;
            this.easing = easing;
            this.startY = startY;
            this.endY = endY;
            this.timingGroup = timingGroup;
        }

        public float2 PositionAt(int time) => PositionAt(time, timing, endTiming, StartPos, EndPos, easing);
        public Circle2D ColliderAt(int time) => ColliderAt(time, timing, endTiming, StartPos, EndPos, easing);

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
