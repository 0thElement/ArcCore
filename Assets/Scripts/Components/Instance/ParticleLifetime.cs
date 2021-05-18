using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ParticleLifetime : IComponentData
    {
        public int value;
        public ParticleLifetime(int value)
            => this.value = value;
    }
}
