using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ColorID : IComponentData
    {
        public int value;
        public ColorID(int value)
            => this.value = value;
    }
}
