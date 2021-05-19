using ArcCore;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent, System.Obsolete(null, error: true)]
    public struct ChartTimeEnd : IComponentData
    {
        public int value;

        public ChartTimeEnd(int value) => this.value = value;
    }
}
