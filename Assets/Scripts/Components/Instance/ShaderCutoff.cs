using Unity.Entities;
using Unity.Rendering;
using Unity.Burst;

namespace ArcCore.Components
{
    [MaterialProperty("_Cutoff", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct ShaderCutoff : IComponentData
    {
        public float Value;
    }
}
