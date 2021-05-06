using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct AppearTime : IComponentData
    {
        public int Value;
    }
}