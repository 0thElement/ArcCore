using Unity.Mathematics;

namespace ArcCore.Utility
{
    public readonly struct AABB2D
    {
        public readonly float2 min;
        public readonly float2 max;

        public AABB2D(float2 a, float2 b)
        {
            min = math.min(a, b);
            max = math.max(a, b);
        }

        private AABB2D(float ax, float ay, float bx, float by)
        {
            min = new float2(ax, ay);
            max = new float2(bx, by);
        }

        public bool CollidesWith(AABB2D other)
            => min.x <= other.max.x && max.x >= other.min.x
            && min.y <= other.max.y && max.y >= other.min.y;

        public bool IsNone
            => float.IsNaN(min.x);

        public static readonly AABB2D none = new AABB2D(float.NaN, float.NaN, float.NaN, float.NaN);
    }
}
