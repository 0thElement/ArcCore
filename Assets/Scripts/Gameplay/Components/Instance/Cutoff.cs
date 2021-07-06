using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct Cutoff : IComponentData
    {
        public bool value;
        public Cutoff (bool value)
            => this.value = value;
    }
}