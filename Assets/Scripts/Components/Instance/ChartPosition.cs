using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartPosition : IComponentData
    {
        public float2 xy;
        public ChartPosition(float2 xy) => 
            this.xy = xy;
    }
}
