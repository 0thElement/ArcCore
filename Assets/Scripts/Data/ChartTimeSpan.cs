using ArcCore;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ChartTimeSpan : IComponentData
    {
        public int start;
        public int end;

        public ChartTimeSpan(int s, int e) => (start, end) = (s, e);
        public void Deconstruct(out int s, out int e) => (s, e) = (start, end);

        public bool CheckSpan(int currentTime, int leadin = Constants.LostWindow, int leadout = Constants.LostWindow)
            => start - leadin <= currentTime && currentTime <= end + leadout;
        public int GetHoldCount(double appendTime)
            => (int)((end - start) / appendTime) - 1;
    }
}
