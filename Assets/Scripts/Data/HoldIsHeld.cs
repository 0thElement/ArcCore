using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct HoldIsHeld : IComponentData
    {
        public bool value;
        public HoldIsHeld(bool v) 
            => value = v;
    }

    [GenerateAuthoringComponent]
    public struct HoldVisualHeld : IComponentData
    {
        public bool value;
        public HoldVisualHeld(bool v)
            => value = v;
    }
}
