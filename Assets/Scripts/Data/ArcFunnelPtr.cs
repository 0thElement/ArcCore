using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public unsafe struct ArcFunnelPtr : IComponentData
    {
        public ArcFunnel* Value;
    }
}
