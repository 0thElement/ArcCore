using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct HitState : IComponentData
    {
        public float Value;
        public bool HitRaw;
    }
}
