using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ArcGroupStartTime : IComponentData
    {
        public int value;
        public ArcGroupStartTime(int v) => value = v;
    }
}
