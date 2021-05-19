using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ColorID : IComponentData
    {
        public int value;
        public ColorID(int value)
            => this.value = value;
    }
}
