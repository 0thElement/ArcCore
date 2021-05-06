using ArcCore;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartTimeSpan : IComponentData
    {
        public int start;
        public int end;

        public ChartTimeSpan(int s, int e) => (start, end) = (s, e);
        public void Deconstruct(out int s, out int e) => (s, e) = (start, end);

        public int GetHoldCount(double appendTime)
            => (int)((end - start) / appendTime) - 1;
    }
}
