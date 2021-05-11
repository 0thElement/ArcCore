using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartIncrTime : IComponentData
    {
        /// <summary>
        /// The current time of the judge point.
        /// </summary>
        public float time;

        /// <summary>
        /// The distance, in ms between two judge points.
        /// </summary>
        public readonly int timeIncrement;
        /// <summary>
        /// The time at which the item is considered "finished" (no mroe judge points)
        /// </summary>
        public readonly int endTime;

        public ChartIncrTime(int startTime, int endTime, int inc) => (time, this.endTime, timeIncrement) = (startTime, endTime, inc);
        
        /// <summary>
        /// Create a new <see cref="ChartIncrTime"/> from a start time, end time and bpm.
        /// The <c>out</c> parameter <paramref name="count"/> will be set to the amount of judge points which the new
        /// instance will take before <see cref="UpdateJudgePointCache(int, bool, out int)"/> will return false.
        /// </summary>
        public static ChartIncrTime FromBpm(int startTime, int endTime, float bpm, out int count)
        {
            int i = GetIncr(bpm);
            count = (endTime - startTime) / i;
            return new ChartIncrTime(startTime, endTime, i);
        }

        public static int GetIncr(float bpm)
        {
            int v = (int)math.abs((bpm >= 255 ? 60_000f : 30_000f) / bpm);
            return math.max(1, v);
        }

        /// <summary>
        /// Update the field <c>time</c> of this instance to reflect the next judge point in the hold or arc.
        /// If <paramref name="lostExpected"/>, do not advance past any points that may still be valid.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the hold has reached its end
        /// </returns>
        [BurstCompile(FloatMode = FloatMode.Fast)] 
        public bool UpdateJudgePointCache(int ctime, bool lostExpected, out int count)
        {
            if (lostExpected) ctime -= Constants.FarWindow;

            count = (int)(1 + (ctime - time) / timeIncrement);
            time += timeIncrement * count;

            return time + timeIncrement <= endTime;
        }
    }
}
