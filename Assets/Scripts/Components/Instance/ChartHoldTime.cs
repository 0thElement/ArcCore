using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartHoldTime : IComponentData
    {
        public double time;
        public readonly double timeIncrement;

        public int iteration;

        public ChartHoldTime(ChartTimeSpan span, double inc) => (time, timeIncrement, iteration) = (span.start, inc, 0);

        /// <returns>
        /// <c>false</c> if the hold has reached its end
        /// </returns>
        public bool Increment(ChartTimeSpan span)
        {
            iteration++;
            double newacc = span.start + timeIncrement * iteration;

            //Skip last timing 
            //TODO: (CHECK WITH 0th)
            if (newacc + timeIncrement >= span.end) return false;

            time = newacc;
            return true;
        }

        public bool CheckSpan(int currentTime, int leadin = Constants.LostWindow, int leadout = Constants.LostWindow)
            => time - leadin <= currentTime && currentTime <= time + leadout;
        public bool CheckStart(int currentTime, int leadin = Constants.LostWindow)
            => time - leadin <= currentTime;
        public bool CheckOutOfRange(int currentTime)
            => time + Constants.FarWindow < currentTime;
    }
}
