using Unity.Mathematics;

namespace ArcCore.Gameplay.Mathematics
{
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
