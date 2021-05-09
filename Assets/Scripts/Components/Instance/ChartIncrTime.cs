using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ChartIncrTime : IComponentData
    {
        public float time;
        public readonly float timeIncrement;

        public ChartIncrTime(int startTime, float inc) => (time, timeIncrement) = (startTime, inc);
        public static ChartIncrTime FromBpm(int startTime, int endTime, float bpm, out int count)
        {
            float i = GetIncr(bpm);
            count = (int)((endTime - startTime) / i);
            return new ChartIncrTime(startTime, i);
        }

        public static float GetIncr(float bpm)
            => (bpm >= 255 ? 60_000f : 30_000f) / bpm;

        /// <returns>
        /// <c>false</c> if the hold has reached its end
        /// </returns>
        [BurstCompile(FloatMode = FloatMode.Fast)] public bool Increment(int end, int ctime)
        {
            time += timeIncrement * (int)(1 + (ctime - time) / timeIncrement);

            //Skip last timing 
            //TODO: (CHECK WITH 0th)
            return time + timeIncrement <= end;
        }

        public bool CheckSpan(int currentTime, int leadin = Constants.LostWindow, int leadout = Constants.LostWindow)
            => time - leadin <= currentTime && currentTime <= time + leadout;
        public bool CheckStart(int currentTime, int leadin = Constants.LostWindow)
            => time - leadin <= currentTime;
        public bool CheckOutOfRange(int currentTime)
            => time + Constants.FarWindow < currentTime;
    }
}
