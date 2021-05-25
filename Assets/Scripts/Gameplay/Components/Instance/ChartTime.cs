using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ChartTime : IComponentData
    {
        public int value;
        public ChartTime(int v) => value = v;
    }
}
