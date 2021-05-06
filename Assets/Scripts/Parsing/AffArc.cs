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
