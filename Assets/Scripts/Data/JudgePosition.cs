using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct JudgePosition : IComponentData
    {
        public float2 position;
    }
}
