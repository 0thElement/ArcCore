using System;

namespace ArcCore.Utilities
{
    public static class TimeUtils
    {
        public static float TicksToSec(long ticks) => ticks / 10_000_000f;
        public static long SecToTicks(float sec) => (long)(sec * 10_000_000);
    }
}