using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct SinglePosition : IComponentData
    {
        public float2 position;
    }
}
