using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct FloorPosition : IComponentData
    {
        public float Value;
    }

}