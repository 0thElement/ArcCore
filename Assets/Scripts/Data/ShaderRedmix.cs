using Unity.Entities;
using Unity.Rendering;
using Unity.Burst;

namespace ArcCore.Data
{
    [MaterialProperty("_RedMix", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct ShaderRedmix : IComponentData
    {
        public float Value;
    }
}
