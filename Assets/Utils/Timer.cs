using System;
using Unity.Mathematics;

namespace UnityCoroutineUtils
{
    public class Timer
    {
        private Timer(DateTimeOffset start, TimeSpan length)
        {
            this.start = start;
            this.length = length;
        }

        public static Timer StartNew(TimeSpan length)
            => new Timer(DateTimeOffset.Now, length);

        public readonly DateTimeOffset start;
        public readonly TimeSpan length;
             
        public TimeSpan Elapsed => DateTimeOffset.Now - start;
        public float PercentComplete => (float)math.clamp(Elapsed.TotalSeconds / length.TotalSeconds, 0, 1);

        public bool IsComplete => Elapsed >= length;
    }
}
