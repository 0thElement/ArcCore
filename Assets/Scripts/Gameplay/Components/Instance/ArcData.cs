using ArcCore.Parsing.Aff;
using Unity.Entities;
using Unity.Mathematics;
using ArcCore.Math;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ArcData : IComponentData
    {
        public float2 start;
        public float2 end;
        public int startTiming;
        public int endTiming;
        public ArcEasing easing;

        public ArcData(float2 start, float2 end, int startTiming, int endTiming, ArcEasing easing)
        {
            this.start = start;
            this.end = end;
            this.easing = easing;
            this.startTiming = startTiming;
            this.endTiming = endTiming;
        }

        public bool CollideWith(int currentTiming, Rect2D rect)
        {
            float t = (float)(currentTiming - startTiming) / (endTiming - startTiming);
            float2 currentPos = Conversion.GetPosAt(t, start, end, easing);

            //i might be a bit stupid
            if (currentPos.y < 5.5f && rect.min.y > 5.5f) rect = new Rect2D(rect.min.x, 5.5f, rect.max.x, rect.max.y);
            return rect.CollidesWith(new Rect2D(currentPos + Constants.ArcBoxExtents, currentPos - Constants.ArcBoxExtents));
        }
    }
}