using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ColorID : IComponentData
    {
        public int Value;
    }
}
