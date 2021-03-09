using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct HoldHeldJudge : IComponentData
    {
        public bool Value;
    }
}
