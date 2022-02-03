using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Gameplay.Parsing;
using ArcCore.Gameplay.Objects.Particle;
using ArcCore.Gameplay.Data;
using ArcCore.Utilities;
using ArcCore.Utilities.Extensions;
using Unity.Entities.Serialization;

namespace ArcCore.Gameplay.EntityCreation
{
    public class TraceEntityCreator : ArclikeEntityCreator
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

            // traceNoteEntityPrefab     = gocs.ConvertToNote(traceNotePrefab, em);
            // traceShadowEntityPrefab   = gocs.ConvertToNote(traceShadowPrefab, em);
            // headTraceNoteEntityPrefab = gocs.ConvertToNote(headTraceNotePrefab, em);

            em.ExposeLocalToWorld(traceNoteEntityPrefab);
            em.ExposeLocalToWorld(traceShadowEntityPrefab);
        }

        public void CreateEntities(IChartParser parser)
        {
            CreateArclike(parser.Traces);
        }

        protected override void CreateSegment(ArcRaw arc, float3 start, float3 end, int time, int endTime, int groupID)
        {
            Entity traceEntity = em.Instantiate(traceNoteEntityPrefab);

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, arc.timingGroup);
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
            em.SetComponentData(traceEntity, new TimingGroup(arc.timingGroup));
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
                em.SetComponentData(traceShadowEntity, new TimingGroup(arc.timingGroup));
                em.SetComponentData(traceShadowEntity, new AppearTime(appearTime));
                em.SetComponentData(traceShadowEntity, new DestroyOnTiming(endTime));
                em.SetComponentData(traceShadowEntity, new ChartTime(time));
            }
        }

        protected override void CreateHeadSegment(ArcRaw trace, int _)
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
        protected override void SetupIndicators(List<ArcPointData> connectedArcsIdEndpoint)
        {
            List<IIndicator> indicatorList = new List<IIndicator>(connectedArcsIdEndpoint.Count);

            foreach (var groupIdEndPoint in connectedArcsIdEndpoint)
            {
                TraceIndicator indicator = new TraceIndicator(Object.Instantiate(traceApproachIndicatorPrefab), groupIdEndPoint.time);
                indicatorList.Add(indicator);
            }
            PlayManager.TraceIndicatorHandler.Initialize(indicatorList);
        }
        protected override void CreateHeightIndicator(ArcRaw arc) {}
        protected override void CreateJudgeEntity(ArcRaw arc, int groupId, float startBpm) {}
    }
}