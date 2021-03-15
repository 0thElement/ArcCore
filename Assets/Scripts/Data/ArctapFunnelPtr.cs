using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public unsafe struct ArctapFunnelPtr : IComponentData
    {
        public ArctapFunnel* Value;
    }
}
