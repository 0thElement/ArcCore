using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ParticleType : IComponentData
    {
        public enum Value
        {
            Judge
        }

        public Value value;
        public ParticleType(Value value)
            => this.value = value;
    }
}
