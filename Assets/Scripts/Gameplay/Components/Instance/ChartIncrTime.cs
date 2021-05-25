using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ChartIncrTime : IComponentData
    {
        /// <summary>
        /// The current time of the judge point.
        /// </summary>
        public int time;

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
            count = (int)math.ceil((float)(endTime - startTime) / i) - 1;
            return new ChartIncrTime(startTime, endTime, i);
        }

        public static int GetIncr(float bpm)
        {
            int v = (int)math.abs((bpm >= 255 ? 60_000f : 30_000f) / bpm);
            return math.max(1, v);
        }

        /// <summary>
        /// Update the field <c>time</c> of this instance to reflect the next judge point in the hold or arc.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the hold has reached its end
        /// </returns>
        [BurstCompile(FloatMode = FloatMode.Fast)] 
        public bool UpdateJudgePointCache(int ctime, out int count)
        {
            count = math.max(1, (ctime - time - Constants.FarWindow) / timeIncrement);
            time += timeIncrement * count;

            return time + timeIncrement < endTime;
        }

        /// <summary>
        /// Update the field <c>time</c> of this instance to reflect the next judge point in the hold or arc.
        /// </summary>
        /// <returns>
        /// <c>false</c> if the hold has reached its end
        /// </returns>
        [BurstCompile(FloatMode = FloatMode.Fast)]
        public bool UpdateJudgePointCachePure(int ctime, out int count)
        {
            count = math.max(1, (ctime - time) / timeIncrement);
            time += timeIncrement * count;

            return time + timeIncrement < endTime;
        }
    }
}
