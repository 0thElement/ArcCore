using Unity.Mathematics;

namespace ArcCore.Structs
{
    public readonly struct ComboParticleAction
    {
        public enum Type
        {
            LATE = SkyParticleAction.Type.___len,
            EARLY,

            ___len
        }

        public readonly Type type;
        public static readonly float3 pos = new float3(0,10f,0); //TEMPORARY
    }
}
