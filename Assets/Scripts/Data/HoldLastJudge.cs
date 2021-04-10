using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct HoldLastJudge : IComponentData
    {
        public bool value;
        public HoldLastJudge(bool v) 
            => value = v;
    }
}
