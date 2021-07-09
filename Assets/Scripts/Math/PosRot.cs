using Unity.Mathematics;
using System.Diagnostics.CodeAnalysis;

namespace ArcCore.Math
{
    public readonly struct PosRot
    {
        public PosRot(float3 pos, float3 rot)
        {
            position = pos;
            rotation = rot;
        }

        public PosRot(float xpos, float ypos, float zpos, float xrot, float yrot, float zrot)
        {
            position = new float3(xpos, ypos, zpos);
            rotation = new float3(xrot, yrot, zrot);
        }

        public readonly float3 position;
        public readonly float3 rotation;

        public static PosRot operator +(PosRot a, PosRot b)
            => new PosRot(a.position + b.position, a.rotation + b.rotation);
        public static PosRot operator -(PosRot a, PosRot b)
            => new PosRot(a.position - b.position, a.rotation - b.rotation);
        public static PosRot operator *(PosRot a, PosRot b)
            => new PosRot(a.position * b.position, a.rotation * b.rotation);
        public static PosRot operator *(PosRot a, float b)
            => new PosRot(a.position * b, a.rotation * b);
        public static PosRot operator /(PosRot a, PosRot b)
            => new PosRot(a.position / b.position, a.rotation - b.rotation);
        public static PosRot operator /(PosRot a, float b)
            => new PosRot(a.position / b, a.rotation / b);

        [SuppressMessage("Style", "IDE1006:Naming Styles")]
        public static PosRot lerp(PosRot a, PosRot b, float t)
        {
            return new PosRot(
                math.lerp(a.position, b.position, t), 
                math.lerp(a.rotation, b.rotation, t)
            );
        }

        public override string ToString()
            => $"(pos: {position}, rot: {rotation})";
    }
}
