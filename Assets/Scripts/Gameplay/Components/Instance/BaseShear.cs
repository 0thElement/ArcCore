using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct BaseShear : IComponentData
    {
        public float4 value;
        public BaseShear (float4 value)
            => this.value = value;
    }
}