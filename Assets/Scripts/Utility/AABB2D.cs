using Unity.Mathematics;

namespace ArcCore.Utility
{
    public struct AABB2D
    {
        public float2 Center;
        public float2 Extents;

        public AABB2D(float2 center, float2 extents)
        {
            Center = center;
            Extents = extents;
        }

        public float2 TopLeft => new float2(Center.x - Extents.x, Center.y + Extents.y);
        public float2 TopRight => Center + Extents;
        public float2 BottomLeft => Center - Extents;
        public float2 BottomRight => new float2(Center.x + Extents.x, Center.y - Extents.y);

        public float MinX => Center.x - Extents.x;
        public float MaxX => Center.x + Extents.x;
        public float MinY => Center.y - Extents.y;
        public float MaxY => Center.y + Extents.y;

        public bool CollidesWith(AABB2D other)
            => MinX <= other.MaxX && MaxX >= other.MinX
            && MinY <= other.MaxY && MaxY >= other.MinY;

        public (int minT, int maxT) GetTracks()
            => (Convert.XToTrack(MinX), Convert.XToTrack(MaxX));

        public static AABB2D FromCorners(float2 tl, float2 br)
            => new AABB2D((tl + br) / 2, math.abs(tl - br));
    }
}
