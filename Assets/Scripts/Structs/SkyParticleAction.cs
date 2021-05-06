using Unity.Mathematics;

namespace ArcCore.Structs
{
    public readonly struct SkyParticleAction
    {
        public enum Type
        {
            ARCHELD,
            TAP,

            PURE,
            FAR,
            LOST,

            ___len
        }

        public readonly Type type;
        private readonly float2 position;
    }
}
