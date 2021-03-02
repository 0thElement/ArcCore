using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct JudgeTime : IComponentData
    {
        public int time;
    }
}
