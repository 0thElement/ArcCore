using ArcCore;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartTimeEnd : IComponentData
    {
        public int value;

        public ChartTimeEnd(int value) => this.value = value;
    }
}
