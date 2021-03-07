using Unity.Entities;

namespace ArcCore.Data
{
    public struct ArcReference : IComponentData
    {
        public Entity arcEntity;
    }
}
