using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct DisappearTime : IComponentData
    {
        public int Value;
    }
}