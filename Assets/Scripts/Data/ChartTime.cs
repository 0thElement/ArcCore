using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ChartTime : IComponentData
    {
        public int Value;
    }
}
