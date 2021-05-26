using Unity.Mathematics;

namespace ArcCore.Parsing.Aff
{
    public struct AffArcTap
    {
        public int timing;
        public float2 position;
        public int timingGroup;

        public AffArcTap(int timing, float2 position, int timingGroup)
        {
            this.timing = timing;
            this.position = position;
            this.timingGroup = timingGroup;
        }
    }
}
