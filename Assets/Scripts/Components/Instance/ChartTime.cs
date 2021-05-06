using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartTime : IComponentData
    {
        public int value;
        public ChartTime(int v) => value = v;
        public bool CheckSpan(int currentTime, int leadin = Constants.LostWindow, int leadout = Constants.LostWindow)
            => value - leadin <= currentTime && currentTime <= value + leadout;
        public bool CheckStart(int currentTime, int leadin = Constants.LostWindow)
            => value - leadin <= currentTime;
        public bool CheckOutOfRange(int currentTime)
            => value + Constants.FarWindow < currentTime;
    }
}
