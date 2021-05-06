using Unity.Mathematics;

namespace ArcCore.Parsing
{
    public struct AffCamera
    {
        public int timing;
        public float3 position;
        public float3 rotate;
        public CameraEasing easing;
        public int duration;
    }
}
