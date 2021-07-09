using ArcCore.Gameplay;
using ArcCore.Math;
using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Parsing.Aff
{
    public struct AffCamera
    {
        private int _timing;
        public int Timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        public PosRot target;
        public CameraEasing easing;

        private int _duration;
        public int Duration
        {
            get => _duration;
            set => _duration = GameSettings.GetSpeedModifiedTime(value);
        }

        public int EndTiming => Timing + Duration;

        public float LerpValue(int time)
            => math.unlerp(_timing, EndTiming, time);

        public static float3 TransformPos(float3 p) => new float3(-p.x, p.y, p.z) / 100;
        public static float3 TransformRot(float3 r) => new float3(-r.y, -r.x, r.z);

        public float3 PosFromParam
        {
            set => target = new PosRot(TransformPos(value), target.rotation);
        }
        public float3 RotFromParam
        {
            set => target = new PosRot(target.position, TransformRot(value));
        }

        public void SetStart(PosRot start)
        {
            this.start = start;
            current = start;
        }

        public PosRot start;
        public PosRot last;
        public PosRot delta;
        public PosRot totalDelta;
        public PosRot current;

        public PosRot Distance => target - start;
        public PosRot Remaining => target - (start + totalDelta);

        public PosRot GetAt(int time)
        {
            float t = Conversion.TransformCamPercent(LerpValue(time), easing);
            return PosRot.lerp(start, target, t);
        }
        public PosRot RemainingAt(int time)
            => target - GetAt(time);

        public void Update(int time)
        {
            last = current;
            current = GetAt(time);

            delta = current - last;
            totalDelta += delta;
        }
    }
}
