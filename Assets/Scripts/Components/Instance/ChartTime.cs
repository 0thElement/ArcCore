using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartTime : IComponentData
    {
        public int value;
        public ChartTime(int v) => value = v;
    }
}
