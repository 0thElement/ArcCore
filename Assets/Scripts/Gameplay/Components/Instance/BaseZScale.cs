using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct BaseZScale : IComponentData
    {
        public float value;
        public BaseZScale(float value)
            => this.value = value;
    }
}