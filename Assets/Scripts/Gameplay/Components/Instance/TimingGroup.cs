using Unity.Entities;

namespace ArcCore.Gameplay.Components
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