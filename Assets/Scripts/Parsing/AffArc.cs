using ArcCore.Math;
using ArcCore.Utility;
using Unity.Mathematics;

namespace ArcCore.Parsing
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

        public float2 PositionAt(int time)
            => new float2(
                    Conversion.GetXAt(
                        math.unlerp(time, timing, endTiming),
                        startX, endX, easing
                    ),
                    Conversion.GetYAt(
                        math.unlerp(time, timing, endTiming),
                        startY, endY, easing
                    )
               );
        public Circle2D ColliderAt(int time)
            => new Circle2D(PositionAt(time), Constants.ArcColliderRadius);
    }
}
