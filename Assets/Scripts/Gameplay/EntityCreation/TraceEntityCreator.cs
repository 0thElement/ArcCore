using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Parsing.Data;
using ArcCore.Utilities.Extensions;
using ArcCore.Parsing;
using ArcCore.Gameplay.Objects.Particle;

namespace ArcCore.Gameplay.EntityCreation
{
    public class TraceEntityCreator
    {
        private Entity traceNoteEntityPrefab;
        private Entity traceShadowEntityPrefab;
        private Entity headTraceNoteEntityPrefab;

        private GameObject traceApproachIndicatorPrefab;

        private EntityManager em;

        public TraceEntityCreator(
            World world, GameObject traceNotePrefab, GameObject headTraceNotePrefab, GameObject traceApproachIndicatorPrefab, GameObject traceShadowPrefab)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            this.traceApproachIndicatorPrefab = traceApproachIndicatorPrefab;

            traceNoteEntityPrefab     = gocs.ConvertToNote(traceNotePrefab, em);
            traceShadowEntityPrefab   = gocs.ConvertToNote(traceShadowPrefab, em);
            headTraceNoteEntityPrefab = gocs.ConvertToNote(headTraceNotePrefab, em);

            em.ExposeLocalToWorld(traceNoteEntityPrefab);
            em.ExposeLocalToWorld(traceShadowEntityPrefab);
        }
        //Similar to arc creation
        public void CreateEntities(IChartParser parser)
        {
            var traces = parser.Traces;

            traces.Sort((item1, item2) => item1.timing.CompareTo(item2.timing));
            List<float4> connectedTracesIdEndpoint = new List<float4>();

            foreach (TraceRaw trace in traces)
            {
                float4 traceStartPoint = new float4((float)trace.timingGroup, (float)trace.timing, trace.startX, trace.startY);
                float4 traceEndPoint = new float4((float)trace.timingGroup, (float)trace.endTiming, trace.endX, trace.endY);
                int traceId = -1;
                bool isHeadTrace = true;

                for (int id = connectedTracesIdEndpoint.Count - 1; id >= 0; id--)
                {
                    if (connectedTracesIdEndpoint[id].Equals(traceStartPoint))
                    {
                        traceId = id;
                        isHeadTrace = false;
                        connectedTracesIdEndpoint[id] = traceEndPoint;
                        break;
                    }
                }

                if (isHeadTrace)
                {
                    traceId = connectedTracesIdEndpoint.Count;
                    connectedTracesIdEndpoint.Add(traceEndPoint);
                    CreateHeadSegment(trace);
                }

                int duration = trace.endTiming - trace.timing;

                if (duration == 0)
                {
                    float3 tstart = new float3(
                        Conversion.GetWorldX(trace.startX),
                        Conversion.GetWorldY(trace.startY),
                        PlayManager.Conductor.GetFloorPositionFromTiming(trace.timing, trace.timingGroup)
                    );
                    float3 tend = new float3(
                        Conversion.GetWorldX(trace.endX),
                        Conversion.GetWorldY(trace.endY),
                        PlayManager.Conductor.GetFloorPositionFromTiming(trace.endTiming, trace.timingGroup)
                    );
                    CreateSegment(tstart, tend, trace.timingGroup, trace.timing, trace.endTiming, traceId);
                    continue;
                }

                int v1 = duration < 1000 ? 14 : 7;
                float v2 = 1f / (v1 * duration / 1000f);
                float segmentLength = duration * v2;
                int segmentCount = (int)(duration / segmentLength) + 1;


                float3 start;
                float3 end = new float3(
                    Conversion.GetWorldX(trace.startX),
                    Conversion.GetWorldY(trace.startY),
                    PlayManager.Conductor.GetFloorPositionFromTiming(trace.timing, trace.timingGroup)
                );

                for (int i=0; i<segmentCount - 1; i++)
                {
                    int t = (int)((i + 1) * segmentLength);
                    start = end;
                    end = new float3(
                        Conversion.GetWorldX(Conversion.GetXAt((float)t / duration, trace.startX, trace.endX, trace.easing)),
                        Conversion.GetWorldY(Conversion.GetYAt((float)t / duration, trace.startY, trace.endY, trace.easing)),
                        PlayManager.Conductor.GetFloorPositionFromTiming(trace.timing + t, trace.timingGroup)
                    );

                    CreateSegment(start, end, trace.timingGroup, trace.timing + (int)(i * segmentLength), trace.timing + (int)((i+1) * segmentLength), traceId);
                }

                start = end;
                end = new float3(
                    Conversion.GetWorldX(trace.endX),
                    Conversion.GetWorldY(trace.endY),
                    PlayManager.Conductor.GetFloorPositionFromTiming(trace.endTiming, trace.timingGroup)
                );

                CreateSegment(start, end, trace.timingGroup, (int)(trace.endTiming - segmentLength), trace.endTiming, traceId);
            }
            
            List<IIndicator> indicatorList = new List<IIndicator>(connectedTracesIdEndpoint.Count);

            foreach (float4 groupIdEndPoint in connectedTracesIdEndpoint)
            {
                TraceIndicator indicator = new TraceIndicator(Object.Instantiate(traceApproachIndicatorPrefab), (int)groupIdEndPoint.y);
                indicatorList.Add(indicator);
            }
            PlayManager.TraceIndicatorHandler.Initialize(indicatorList);
        }

        private void CreateSegment(float3 start, float3 end, int timingGroup, int time, int endTime, int groupID)
        {
            Entity traceEntity = em.Instantiate(traceNoteEntityPrefab);

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            //Shear along xy + scale along z matrix
            em.SetComponentData(traceEntity, new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, dy, start.y,
                    0, 0, dz, 0,
                    0, 0, 0,  1
                )
            });
            em.SetComponentData(traceEntity, new FloorPosition(start.z));
            em.SetComponentData(traceEntity, new TimingGroup(timingGroup));
            em.SetComponentData(traceEntity, new BaseOffset(new float4(start.x, start.y, 0, 0)));
            em.SetComponentData(traceEntity, new BaseShear(new float4(dx, dy, dz, 0)));
            em.SetComponentData(traceEntity, new Cutoff(true));
            em.SetComponentData(traceEntity, new AppearTime(appearTime));
            em.SetComponentData(traceEntity, new DestroyOnTiming(endTime));
            em.SetComponentData(traceEntity, new ChartTime(time));
            em.SetComponentData(traceEntity, new ChartEndTime(endTime));
            em.SetComponentData(traceEntity, new ArcGroupID(groupID));

            if (time < endTime)
            {
                Entity traceShadowEntity = em.Instantiate(traceShadowEntityPrefab);

                em.SetComponentData(traceShadowEntity, new FloorPosition() { value = start.z });
                em.SetComponentData(traceShadowEntity, new LocalToWorld()
                {
                    Value = new float4x4(
                        1, 0, dx, start.x,
                        0, 1, 0,  0,
                        0, 0, dz, 0,
                        0, 0, 0,  1
                    )
                });
                em.SetComponentData(traceShadowEntity, new BaseOffset(new float4(start.x, 0, 0, 0)));
                em.SetComponentData(traceShadowEntity, new BaseShear(new float4(dx, 0, dz, 0)));
                em.SetComponentData(traceShadowEntity, new Cutoff(true));
                em.SetComponentData(traceShadowEntity, new TimingGroup(timingGroup));
                em.SetComponentData(traceShadowEntity, new AppearTime(appearTime));
                em.SetComponentData(traceShadowEntity, new DestroyOnTiming(endTime));
                em.SetComponentData(traceShadowEntity, new ChartTime(time));
            }
        }

        private void CreateHeadSegment(TraceRaw trace)
        {
            Entity headEntity = em.Instantiate(headTraceNoteEntityPrefab);

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(trace.timing, trace.timingGroup);

            float x = Conversion.GetWorldX(trace.startX); 
            float y = Conversion.GetWorldY(trace.startY); 
            const float z = 0;
            
            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, 0);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, 0);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(headEntity, new FloorPosition(floorpos));
            em.SetComponentData(headEntity, new Translation() { Value = new float3(x, y, z) });
            em.SetComponentData(headEntity, new TimingGroup(trace.timingGroup));
            em.SetComponentData(headEntity, new AppearTime(appearTime));
        }
    }
}