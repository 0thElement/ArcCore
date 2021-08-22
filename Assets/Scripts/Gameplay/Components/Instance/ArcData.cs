using ArcCore.Parsing.Data;
using Unity.Entities;
using Unity.Mathematics;
using ArcCore.Math;
using UnityEngine;

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
            float2 currentPos = GetPosAt(currentTiming);

            //i might be a bit stupid
            if (currentPos.y < 5.5f && rect.min.y > 5.5f) rect = new Rect2D(rect.min.x, 5.5f, rect.max.x, rect.max.y);
            return rect.CollidesWith(new Rect2D(currentPos + Constants.ArcBoxExtents, currentPos - Constants.ArcBoxExtents));
        }

        public float2 GetPosAt(int timing)
        {
            float t = (float)(timing - startTiming) / (endTiming - startTiming);
            t = Mathf.Clamp(t, 0, 1);
            return Conversion.GetPosAt(t, start, end, easing);
        }
    }
}