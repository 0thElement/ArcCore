using ArcCore.Parsing.Aff;
using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ArcData : IComponentData
    {
        public float2 start;
        public float2 end;
        public ArcEasing easing;

        public ArcData(float2 start, float2 end, ArcEasing easing)
        {
            this.start = start;
            this.end = end;
            this.easing = easing;
        }
    }
}