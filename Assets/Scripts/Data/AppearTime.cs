using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct AppearTime : IComponentData
    {
        public int Value;
    }
}