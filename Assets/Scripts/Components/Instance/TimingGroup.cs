using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct TimingGroup : IComponentData
    {
        public int value;

        public TimingGroup(int value)
        {
            this.value = value;
        }
    }
}