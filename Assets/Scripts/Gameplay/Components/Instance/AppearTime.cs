using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct AppearTime : IComponentData
    {
        public int value;
        public AppearTime(int value)
            => this.value = value;
    }
}