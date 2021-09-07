using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ArcTapShadowReference : IComponentData
    {
        public Entity value;
        public ArcTapShadowReference(Entity value)
            => this.value = value;
    }
}
