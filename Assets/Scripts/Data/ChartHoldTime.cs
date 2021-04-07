using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct ChartHoldTime : IComponentData
    {
        public double timeAccumulator;
        public readonly double timeIncrement;

        public int iteration;

        public ChartHoldTime(double acc, double inc) => (timeAccumulator, timeIncrement, iteration) = (acc, inc, 0);

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

            timeAccumulator = newacc;
            return true;
        }
    }
}
