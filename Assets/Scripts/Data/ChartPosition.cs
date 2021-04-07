using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ChartPosition : IComponentData
    {
        public float2 xy;
        public int lane;

        public ChartPosition(float2 xy) => (this.xy, lane) = (xy, 0);
        public ChartPosition(int lane) => (xy, this.lane) = (float2.zero, lane);
    }
}
