using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct BaseLength : IComponentData
    {
        public float value;
        public BaseLength (float value)
            => this.value = value;
    }
}