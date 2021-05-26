using Unity.Mathematics;

namespace ArcCore.Parsing.Aff
{
    public struct AffCamera
    {
        public int timing;
        public float3 position;
        public float3 rotate;
        public CameraEasing easing;
        public int duration;

        public AffCamera(int timing, float3 position, float3 rotate, CameraEasing easing, int duration)
        {
            this.timing = timing;
            this.position = position;
            this.rotate = rotate;
            this.easing = easing;
            this.duration = duration;
        }
    }
}
