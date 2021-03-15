using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public unsafe struct HoldFunnelPtr : IComponentData
    {
        public HoldFunnel* Value;
    }
}
