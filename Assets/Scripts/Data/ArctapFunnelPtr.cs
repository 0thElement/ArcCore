using Unity.Entities;

namespace ArcCore.Data
{
    [System.Obsolete]
    [GenerateAuthoringComponent]
    public unsafe struct ArctapFunnelPtr : IComponentData
    {
        public ArctapFunnel* Value;
    }
}
