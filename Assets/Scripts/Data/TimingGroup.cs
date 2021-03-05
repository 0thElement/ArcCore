using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct TimingGroup : IComponentData
    {
        public int Value;
    }
}