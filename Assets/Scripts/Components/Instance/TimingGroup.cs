using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct TimingGroup : IComponentData
    {
        public int Value;
    }
}