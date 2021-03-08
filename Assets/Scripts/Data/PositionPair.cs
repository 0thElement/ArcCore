using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    public struct PositionPair : IComponentData
    {
        public float2 startPosition;
        public float2 endPosition;
    }
}
