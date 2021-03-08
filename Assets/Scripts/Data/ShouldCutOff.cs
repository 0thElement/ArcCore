using Unity.Entities;
using Unity.Rendering;
using Unity.Burst;

namespace ArcCore.Data
{
    [MaterialProperty("_Cutoff", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct ShouldCutOff : IComponentData
    {
        public float Value;

        [BurstDiscard]
        public static ShouldCutOff FromBool(bool b)
            => new ShouldCutOff() { Value = b ? 1 : 0 };
        [BurstDiscard]
        public void SetBool(bool b)
            => Value = b ? 1 : 0;
    }
}
