using Unity.Mathematics;

namespace ArcCore.Parsing.Data
{
    public struct ArctapRaw
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public float2 position;
        public int timingGroup;
    }
}
