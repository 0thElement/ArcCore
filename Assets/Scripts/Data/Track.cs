using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct Track : IComponentData
    {
        public int lane;
    }
}
