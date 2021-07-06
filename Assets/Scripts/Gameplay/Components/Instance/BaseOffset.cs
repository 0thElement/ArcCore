using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct BaseOffset : IComponentData
    {
        public float4 value;
        public BaseOffset (float4 value)
            => this.value = value;
    }
}