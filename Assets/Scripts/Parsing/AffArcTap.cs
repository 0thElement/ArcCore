using Unity.Mathematics;

namespace ArcCore.Parsing.Aff
{
    public struct AffArcTap
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.GetSpeedModifiedTime(value);
        }

        public float2 position;
        public int timingGroup;
    }
}
