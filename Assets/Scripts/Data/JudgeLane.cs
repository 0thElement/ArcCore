using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct JudgeLane : IComponentData
    {
        public int lane;
    }
}
