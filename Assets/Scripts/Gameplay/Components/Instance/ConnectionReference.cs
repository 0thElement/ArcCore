using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ConnectionReference : IComponentData
    {
        public Entity value;
        public ConnectionReference(Entity value)
            => this.value = value;
    }
}