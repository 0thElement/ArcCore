using Unity.Mathematics;

namespace ArcCore.Parsing.Aff
{
    public struct AffCamera
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        public float3 position;
        public float3 rotate;
        public CameraEasing easing;
        public int duration;
    }
}
