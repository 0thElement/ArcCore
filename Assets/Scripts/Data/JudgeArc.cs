using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct JudgeArc : IComponentData
    {
        public int colorID;
    }
}
