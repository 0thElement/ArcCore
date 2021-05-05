using Unity.Mathematics;

namespace ArcCore.Utility
{
    public readonly struct Rect2D
    {
        public readonly float2 min;
        public readonly float2 max;

        public Rect2D(float2 a, float2 b)
        {
            min = math.min(a, b);
            max = math.max(a, b);
        }

        private Rect2D(float ax, float ay, float bx, float by)
        {
            min = new float2(ax, ay);
            max = new float2(bx, by);
        }

        public bool CollidesWith(Rect2D other)
            => min.x <= other.max.x && max.x >= other.min.x
            && min.y <= other.max.y && max.y >= other.min.y;
        public bool CollidesWith(Circle2D other) 
        {
            if (ContainsPoint(other.center)) return true;

            float2 center = (min + max) / 2;
            float2 intervec = math.clamp(center - other.center, min, max);
            return math.length(intervec) <= other.radius;
        }
        public bool ContainsPoint(float2 pt)
            => min.x <= pt.x && pt.x <= max.x
            && min.y <= pt.y && pt.y <= max.y;

        public bool IsNone
            => float.IsNaN(min.x);

        public static readonly Rect2D none = new Rect2D(float.NaN, float.NaN, float.NaN, float.NaN);
    }

    public readonly struct Circle2D 
    {
        public readonly float2 center;
        public readonly float radius;

        public Circle2D(float2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public bool ContainsPoint(float2 pt)
            => math.length(center - pt) <= radius;
        public bool CollidesWith(Circle2D other)
            => math.length(center - other.center) <= radius + other.radius;
        public bool CollidesWith(Rect2D other)
            => other.CollidesWith(this);
    }
}
