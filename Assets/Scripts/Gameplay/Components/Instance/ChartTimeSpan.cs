using ArcCore;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent, System.Obsolete(null, error: true)]
    public struct ChartTimeEnd : IComponentData
    {
        public int value;
    }
}
