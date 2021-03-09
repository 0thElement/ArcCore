using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    public struct LinearPosGroup : IComponentData
    {
        public float2 startPosition;
        public int startTime;
        public float2 endPosition;
        public int endTime;

        public float2 PosAt(int time)
        {
            float ratio = (float)(time - startTime) / (startTime - endTime);
            return startPosition * (1 - ratio) + endPosition * ratio;
        }
        public float TimeCenter()
            => (startTime + endTime) / 2f;
    }
}
