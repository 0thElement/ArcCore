using System.Collections.Generic;
using Unity.Mathematics;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Gameplay.Data;

namespace ArcCore.Gameplay.EntityCreation
{
    public abstract class ArclikeEntityCreator
    {
        protected int CreateArclike(List<ArcRaw> arcs)
        {
            arcs.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            var connectedArcsIdEndpoint = new List<ArcPointData>();

            foreach (ArcRaw arc in arcs)
            {
                //Precalc and assign a connected group id to avoid having to figure out connection during gameplay
                ArcPointData arcStartPoint = (arc.timingGroup, arc.timing, arc.startX, arc.startY, arc.color);
                ArcPointData arcEndPoint = (arc.timingGroup, arc.endTiming, arc.endX, arc.endY, arc.color);

                int arcId = connectedArcsIdEndpoint.Count;
                bool isHeadArc = true;

                for (int id = connectedArcsIdEndpoint.Count - 1; id >= 0; id--)
                {
                    if (connectedArcsIdEndpoint[id] == arcStartPoint)
                    {
                        arcId = id;
                        isHeadArc = false;
                        connectedArcsIdEndpoint[id] = arcEndPoint;
                        break;
                    }
                }

                if (isHeadArc)
                {
                    connectedArcsIdEndpoint.Add(arcEndPoint);
                    CreateHeadSegment(arc, arcId);
                }

                if (isHeadArc || arc.startY != arc.endY)
                {
                    CreateHeightIndicator(arc);
                }

                float startBpm = PlayManager.Conductor.GetTimingEventFromTiming(arc.timing, arc.timingGroup).bpm;

                //Generate arc segments and shadow segment(each segment is its own entity)
                int duration = arc.endTiming - arc.timing;

                if (duration == 0)
                {
                    float3 tstart = new float3(
                            Conversion.GetWorldX(arc.startX),
                            Conversion.GetWorldY(arc.startY),
                            PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                        );
                    float3 tend = new float3(
                            Conversion.GetWorldX(arc.endX),
                            Conversion.GetWorldY(arc.endY),
                            PlayManager.Conductor.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                        );
                    CreateSegment(arc, tstart, tend, arc.timing, arc.timing, arcId);
                    continue;
                }

                int v1 = duration < 1000 ? 14 : 7;
                float v2 = 1000f / (v1 * duration);
                float segmentLength = duration * v2;
                int segmentCount = (int)(duration / segmentLength) + 1;

                int fromTiming;
                int toTiming = arc.timing;

                float3 start;
                float3 end = new float3(
                        Conversion.GetWorldX(arc.startX),
                        Conversion.GetWorldY(arc.startY),
                        PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                    );

                for (int i = 0; i < segmentCount - 1; i++)
                {
                    int t = (int)((i + 1) * segmentLength);

                    fromTiming = toTiming;
                    toTiming = arc.timing + t;

                    start = end;
                    end = new float3(
                        Conversion.GetWorldX(Conversion.GetXAt((float)t / duration, arc.startX, arc.endX, arc.easing)),
                        Conversion.GetWorldY(Conversion.GetYAt((float)t / duration, arc.startY, arc.endY, arc.easing)),
                        PlayManager.Conductor.GetFloorPositionFromTiming(toTiming, arc.timingGroup)
                    );

                    CreateSegment(arc, start, end, fromTiming, toTiming, arcId);
                }

                fromTiming = toTiming;
                toTiming = arc.endTiming;

                start = end;
                end = new float3(
                    Conversion.GetWorldX(arc.endX),
                    Conversion.GetWorldY(arc.endY),
                    PlayManager.Conductor.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                );

                CreateSegment(arc, start, end, fromTiming, toTiming, arcId);
                CreateJudgeEntity(arc, arcId, startBpm);

            }
            SetupIndicators(connectedArcsIdEndpoint);

            return connectedArcsIdEndpoint.Count;
        }

        protected abstract void CreateSegment(ArcRaw arc, float3 start, float3 end, int timing, int endTiming, int groupId);
        protected abstract void CreateHeightIndicator(ArcRaw arc);
        protected abstract void CreateHeadSegment(ArcRaw arc, int groupID);
        protected abstract void CreateJudgeEntity(ArcRaw arc, int groupId, float startBpm);
        protected abstract void SetupIndicators(List<ArcPointData> connectedArcsIdEndpoint);

    }
}