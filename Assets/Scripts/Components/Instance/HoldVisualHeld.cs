using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct HoldVisualHeld : IComponentData
    {
        public bool value;
        public HoldVisualHeld(bool v)
            => value = v;
    }
}
