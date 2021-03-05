using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct JudgeArc : IComponentData
    {
        public float2 startPosition;
        public float2 endPosition;
        public float endingTime;
        public int colorId;
    }
}
