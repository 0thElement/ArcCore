using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Parsing.Aff;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Behaviours.EntityCreation
{
    //TODO: SOMEHOW ABSTRACT THIS AND MERGE WITH ARC ENTITY CREATOR
    public class TraceEntityCreator : ECSMonoBehaviour
    {
        public static TraceEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject traceNotePrefab;
        [SerializeField] private GameObject headTraceNotePrefab;
        [SerializeField] private GameObject traceShadowPrefab;
        [SerializeField] private GameObject traceApproachIndicatorPrefab;
        [SerializeField] private Material traceMaterial;
        [SerializeField] private Material traceShadowMaterial;
        [SerializeField] private Mesh traceMesh;
        [SerializeField] private Mesh headMesh;
        [SerializeField] private Mesh traceShadowMesh;

        private Entity traceNoteEntityPrefab;
        private Entity traceShadowEntityPrefab;
        private Entity headTraceNoteEntityPrefab;

        private void Awake()
        {
            Instance = this;

            traceNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(traceNotePrefab, EntityManager);
            EntityManager.ExposeLocalToWorld(traceNoteEntityPrefab);

            traceShadowEntityPrefab = GameObjectConversionSettings.ConvertToNote(traceShadowPrefab, EntityManager);
            EntityManager.ExposeLocalToWorld(traceShadowEntityPrefab);

            headTraceNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(headTraceNotePrefab, EntityManager);
        }
        //Similar to arc creation
        public void CreateEntities(List<AffTrace> affTraceList)
        {
            affTraceList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            List<float4> connectedTracesIdEndpoint = new List<float4>();

            foreach (AffTrace trace in affTraceList)
            {
                float4 traceStartPoint = new float4((float)trace.timingGroup, (float)trace.timing, trace.startX, trace.startY);
                float4 traceEndPoint = new float4((float)trace.timingGroup, (float)trace.endTiming, trace.endX, trace.endY);
                int traceId = -1;
                bool isHeadTrace = true;
                for (int id = 0; id < connectedTracesIdEndpoint.Count; id++)
                {
                    if (connectedTracesIdEndpoint[id].Equals(traceStartPoint))
                    {
                        traceId = id;
                        isHeadTrace = false;
                        connectedTracesIdEndpoint[id] = traceEndPoint;
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
                        Conductor.Instance.GetFloorPositionFromTiming(trace.timing, trace.timingGroup)
                    );
                    float3 tend = new float3(
                        Conversion.GetWorldX(trace.endX),
                        Conversion.GetWorldY(trace.endY),
                        Conductor.Instance.GetFloorPositionFromTiming(trace.endTiming, trace.timingGroup)
                    );
                    CreateSegment(tstart, tend, trace.timingGroup, trace.timing, trace.endTiming);
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
                    Conductor.Instance.GetFloorPositionFromTiming(trace.timing, trace.timingGroup)
                );

                for (int i=0; i<segmentCount - 1; i++)
                {
                    int t = (int)((i + 1) * segmentLength);
                    start = end;
                    end = new float3(
                        Conversion.GetWorldX(Conversion.GetXAt((float)t / duration, trace.startX, trace.endX, trace.easing)),
                        Conversion.GetWorldY(Conversion.GetYAt((float)t / duration, trace.startY, trace.endY, trace.easing)),
                        Conductor.Instance.GetFloorPositionFromTiming(trace.timing + t, trace.timingGroup)
                    );

                    CreateSegment(start, end, trace.timingGroup, trace.timing + (int)(i * segmentLength), trace.timing + (int)((i+1) * segmentLength));
                }

                start = end;
                end = new float3(
                    Conversion.GetWorldX(trace.endX),
                    Conversion.GetWorldY(trace.endY),
                    Conductor.Instance.GetFloorPositionFromTiming(trace.endTiming, trace.timingGroup)
                );

                CreateSegment(start, end, trace.timingGroup, (int)(trace.endTiming - segmentLength), trace.endTiming);
            }
            
            List<IIndicator> indicatorList = new List<IIndicator>(connectedTracesIdEndpoint.Count);

            foreach (float4 groupIdEndPoint in connectedTracesIdEndpoint)
            {
                TraceIndicator indicator = new TraceIndicator(Instantiate(traceApproachIndicatorPrefab), (int)groupIdEndPoint.y);
                indicatorList.Add(indicator);
            }
            Conductor.Instance.ArcIndicatorManager.Initialize(indicatorList);
        }

        private void CreateSegment(float3 start, float3 end, int timingGroup, int time, int endTime)
        {
            Entity traceEntity = EntityManager.Instantiate(traceNoteEntityPrefab);

            EntityManager.SetSharedComponentData<RenderMesh>(traceEntity, new RenderMesh()
            {
                mesh = traceMesh,
                material = traceMaterial
            });

            EntityManager.SetComponentData<FloorPosition>(traceEntity, new FloorPosition() { value = start.z });
            EntityManager.SetComponentData(traceEntity, new TimingGroup() { value = timingGroup });

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

            //Shear along xy + scale along z matrix
            EntityManager.SetComponentData<LocalToWorld>(traceEntity, new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, dy, start.y,
                    0, 0, dz, 0,
                    0, 0, 0,  1
                )
            });

            EntityManager.SetComponentData(traceEntity, new BaseOffset(new float4(start.x, start.y, 0, 0)));
            EntityManager.SetComponentData(traceEntity, new BaseShear(new float4(dx, dy, dz, 0)));

            EntityManager.SetComponentData(traceEntity, new Cutoff(true));


            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            EntityManager.SetComponentData(traceEntity, new AppearTime() { value = appearTime });
            EntityManager.SetComponentData(traceEntity, new DestroyOnTiming(endTime));
            EntityManager.SetComponentData(traceEntity, new ChartTime() { value = time });

            if (time < endTime)
            {
                Entity traceShadowEntity = EntityManager.Instantiate(traceShadowEntityPrefab);
                EntityManager.SetSharedComponentData<RenderMesh>(traceShadowEntity, new RenderMesh()
                {
                    mesh = traceShadowMesh,
                    material = traceShadowMaterial
                });
                EntityManager.SetComponentData<FloorPosition>(traceShadowEntity, new FloorPosition() { value = start.z });

                EntityManager.SetComponentData<LocalToWorld>(traceShadowEntity, new LocalToWorld()
                {
                    Value = new float4x4(
                        1, 0, dx, start.x,
                        0, 1, 0,  0,
                        0, 0, dz, 0,
                        0, 0, 0,  1
                    )
                });
                EntityManager.SetComponentData(traceShadowEntity, new BaseOffset(new float4(start.x, 0, 0, 0)));
                EntityManager.SetComponentData(traceShadowEntity, new BaseShear(new float4(dx, 0, dz, 0)));
                EntityManager.SetComponentData(traceShadowEntity, new Cutoff(true));
                EntityManager.SetComponentData(traceShadowEntity, new TimingGroup() { value = timingGroup });
                EntityManager.SetComponentData(traceShadowEntity, new AppearTime() { value = appearTime });
                EntityManager.SetComponentData(traceShadowEntity, new DestroyOnTiming(endTime));
                EntityManager.SetComponentData(traceShadowEntity, new ChartTime() { value = time });
            }
        }

        private void CreateHeadSegment(AffTrace trace)
        {
            Entity headEntity = EntityManager.Instantiate(headTraceNoteEntityPrefab);
            EntityManager.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = traceMaterial
            });
            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(trace.timing, trace.timingGroup);
            EntityManager.SetComponentData(headEntity, new FloorPosition()
            {
                value = floorpos
            });

            float x = Conversion.GetWorldX(trace.startX); 
            float y = Conversion.GetWorldY(trace.startY); 
            const float z = 0;
            EntityManager.SetComponentData(headEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            EntityManager.SetComponentData(headEntity, new TimingGroup()
            {
                value = trace.timingGroup
            });
            
            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, 0);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, 0);
            int appearTime = (t1 < t2) ? t1 : t2;

            EntityManager.SetComponentData(headEntity, new AppearTime()
            {
                value = appearTime
            });
        }
    }
}