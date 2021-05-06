using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct DisappearTime : IComponentData
    {
        public int Value;
    }
}