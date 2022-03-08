using System.Collections.Generic;

namespace ArcCore.Gameplay.Utilities
{
    public class ScopingChunk
    {
        private static SortedSet<int> allChunkAppearTimes = new SortedSet<int>();
        private static List<int> allChunkAppearTimesList = null;
        public static List<int> AllChunkAppearTimes 
        {
            get
            {
                if (allChunkAppearTimesList == null)
                {
                    allChunkAppearTimesList = new List<int>(allChunkAppearTimes);
                }
                return allChunkAppearTimesList;
            } 
        }
        public static void ClearAll()
        {
            allChunkAppearTimes.Clear();
            allChunkAppearTimesList = null;
        }

        public ScopingChunk(int capacity)
        {
            this.capacity = capacity;
        }

        private int capacity;
        private SortedList<int, int> chunks = new SortedList<int, int>();

        public int AddAppearTiming(int timing)
        {
            foreach (int chunkTiming in chunks.Keys)
            {
                if (timing >= chunkTiming)
                {
                    chunks[chunkTiming] += 1;
                    if (chunks[chunkTiming] == capacity)
                        chunks.Remove(chunkTiming);
                    return chunkTiming;
                }
            }

            //No chunk found
            chunks.Add(timing, 1);
            allChunkAppearTimes.Add(timing);
            return timing;
        }
    }
}