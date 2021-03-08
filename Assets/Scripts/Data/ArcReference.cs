using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ArcReference : IComponentData
    {
        public Entity Value;
    }
}
