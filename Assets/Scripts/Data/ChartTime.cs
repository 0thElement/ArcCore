using Unity.Entities;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ChartTime : IComponentData
    {
        public int value;
        public ChartTime(int v) => value = v;
        public bool CheckSpan(int currentTime, int leadin = Constants.LostWindow, int leadout = Constants.LostWindow)
            => value - leadin <= currentTime && currentTime <= value + leadout;
    }
}
