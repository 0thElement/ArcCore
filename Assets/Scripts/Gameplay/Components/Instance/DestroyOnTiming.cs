using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct DestroyOnTiming : IComponentData
    {
        public int value;
        public DestroyOnTiming(int timing) => value = timing;
    }
}
