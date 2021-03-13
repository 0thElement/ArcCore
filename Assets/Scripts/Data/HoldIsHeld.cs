using Unity.Entities;

namespace ArcCore.Data
{
    [System.Obsolete]
    [GenerateAuthoringComponent]
    public struct HoldIsHeld : IComponentData
    {
        public bool Value;
    }
}
