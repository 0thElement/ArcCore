using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartLane : IComponentData
    {
        public int lane;

        public ChartLane(int lane) => 
            this.lane = lane;
    }
}
