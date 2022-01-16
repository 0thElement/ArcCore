using ArcCore.Mathematics;
using Unity.Mathematics;

namespace ArcCore.Parsing.Data
{
    public struct CameraEvent
    {
        private int _timing;
        public int Timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public PosRot targetChange;
        public EasingType easing;

        private int _duration;
        public int Duration
        {
            get => _duration;
            set => _duration = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public int EndTiming => Timing + Duration;

        public float LerpValue(int time)
            => math.unlerp(_timing, EndTiming, time);

        public static float3 TransformPos(float3 p) => new float3(-p.x, p.y, p.z) / 100;
        public static float3 TransformRot(float3 r) => new float3(-r.y, -r.x, r.z);

        public float3 PosChangeFromParam
        {
            set => targetChange = new PosRot(TransformPos(value), targetChange.rotation);
        }
        public float3 RotChangeFromParam
        {
            set => targetChange = new PosRot(targetChange.position, TransformRot(value));
        }

        public PosRot delta;
        private PosRot last;
        private PosRot current;

        public PosRot Remaining => targetChange - current;

        public PosRot GetAt(int time)
        {
            float t = Easing.Do(LerpValue(time), easing);
            return targetChange * t;
        }

        public void Update(int time)
        {
            last = current;
            current = GetAt(time);
            delta = current - last;
        }
    }
}
