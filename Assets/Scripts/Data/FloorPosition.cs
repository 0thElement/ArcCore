using ArcCore.Structs;
using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct FloorPosition : IComponentData
    {
        public fixed_dec Value;
    }

}
