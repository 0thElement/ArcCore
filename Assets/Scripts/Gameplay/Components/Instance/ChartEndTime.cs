
using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ChartEndTime : IComponentData
    {
        public int value;
        public ChartEndTime(int v) => value = v;
    }
}
