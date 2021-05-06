using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ChartPosition : IComponentData
    {
        public float2 xy;
        public ChartPosition(float2 xy) => 
            this.xy = xy;
    }

    [GenerateAuthoringComponent]
    public struct ChartLane : IComponentData
    {
        public int lane;

        public ChartLane(int lane) => 
            this.lane = lane;
    }
}
