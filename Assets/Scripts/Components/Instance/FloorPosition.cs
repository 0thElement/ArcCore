using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct FloorPosition : IComponentData
    {
        public float Value;
    }

}
