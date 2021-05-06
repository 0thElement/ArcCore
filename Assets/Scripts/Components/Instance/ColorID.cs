using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ColorID : IComponentData
    {
        public int Value;
    }
}
