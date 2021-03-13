using Unity.Entities;
using Unity.Rendering;
using Unity.Burst;

namespace ArcCore.Data
{
    [MaterialProperty("_Cutoff", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct ShaderCutoff : IComponentData
    {
        public float Value;
    }
}
