using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct FloorPosition : IComponentData
    {
        public float value;

        public FloorPosition(float value)
        {
            this.value = value;
        }
    }

}
