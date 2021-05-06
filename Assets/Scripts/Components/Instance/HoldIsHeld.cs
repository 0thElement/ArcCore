using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct HoldIsHeld : IComponentData
    {
        public bool value;
        public HoldIsHeld(bool v) 
            => value = v;
    }
}
