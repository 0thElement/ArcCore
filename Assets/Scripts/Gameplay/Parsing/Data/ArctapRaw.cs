using Unity.Mathematics;
using ArcCore.Storage;

namespace ArcCore.Gameplay.Parsing.Data
{
    public struct ArctapRaw
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = Settings.GetSpeedModifiedTime(value);
        }

        public float2 position;
        public int timingGroup;
    }
}
