using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct DisappearTime : IComponentData
    {
        public int value;
        public DisappearTime(int value)
            => this.value = value;
    }
}