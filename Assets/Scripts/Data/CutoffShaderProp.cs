using Unity.Entities;
using Unity.Rendering;

namespace ArcCore.Data
{
    [MaterialProperty("_Cutoff", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct CutoffShaderProp : IComponentData
    {
        public float Value;
    }
}
