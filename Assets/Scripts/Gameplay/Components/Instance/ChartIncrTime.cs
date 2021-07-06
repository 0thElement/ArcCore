using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        /// The time at which the item is considered "finished" (no more judge points)
        /// </summary>
        public readonly int endTime;

        public ChartIncrTime(int startTime, int endTime, int inc) => (time, this.endTime, timeIncrement) = (startTime, endTime, inc);
        
        /// <summary>
        /// Create a new <see cref="ChartIncrTime"/> from a start time, end time and bpm.
        /// The <c>out</c> parameter <paramref name="count"/> will be set to the amount of judge points of the note
        /// </summary>
        public static ChartIncrTime FromBpm(int startTime, int endTime, float bpm, out int count)
        {
            if (bpm == 0)
            {
                count = 0;
                return new ChartIncrTime(startTime, endTime, int.MaxValue);
            }

            int incr = GetIncr(bpm);

            count = math.max(1, (endTime - startTime) / incr - 1);

            int firstJudgeTime = (count == 1) ? (startTime + (endTime - startTime) / 2) : (startTime + incr);
            int finalJudgeTime = firstJudgeTime + (count - 1) * incr;

            return new ChartIncrTime(firstJudgeTime, finalJudgeTime, incr);
        }

        public static int GetIncr(float bpm)
        {
            bpm = math.abs(bpm);
            int v = (int)math.abs((bpm >= 255 ? 60_000f : 30_000f) / bpm);
            return math.max(1, v);
        }

        /// <summary>
        /// Update the field <c>time</c> of this instance to reflect the next judge point in the hold or arc.
        /// </summary>
        /// <returns>
        /// The number <c>count</c> of judge points from <c>time</c> to <c>ctime</c>
        /// </returns>
        [BurstCompile(FloatMode = FloatMode.Fast)]
        public int UpdateJudgePointCache(int ctime)
        {
            int count = (ctime - time) / timeIncrement;
            if (count > 0)
                time += timeIncrement * (count + 1);
            else
                count = 0;

            return count;
        }
    }
}
