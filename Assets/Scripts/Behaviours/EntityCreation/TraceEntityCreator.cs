using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Components;
using ArcCore.Components.Chunk;
using ArcCore.Parsing;
using static ArcCore.EntityManagement;

namespace ArcCore.Behaviours.EntityCreation
{

    public class TraceEntityCreator : MonoBehaviour
    {
        public static TraceEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject traceNotePrefab;
        [SerializeField] private GameObject headTraceNotePrefab;
        [SerializeField] private Material traceMaterial;
        [SerializeField] private Mesh traceMesh;
        [SerializeField] private Mesh headMesh;

        private Entity traceNoteEntityPrefab;
        private Entity headTraceNoteEntityPrefab;
        //private int colorShaderId;

        private void Awake()
        {
            Instance = this;

            traceNoteEntityPrefab = GameObjectToNote(traceNotePrefab);
            ExposeLocalToWorld(traceNoteEntityPrefab);
            EManager.RemoveComponent<ColorID>(traceNoteEntityPrefab);
            EManager.AddComponent(traceNoteEntityPrefab, ComponentType.ReadOnly<ChartTime>());

            headTraceNoteEntityPrefab = GameObjectToNote(headTraceNotePrefab);
            EManager.AddComponent(headTraceNoteEntityPrefab, ComponentType.ReadOnly<ChartTime>());
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
                int v1 = duration < 1000 ? 14 : 7;
                float v2 = 1f / (v1 * duration / 1000f);
                float segmentLength = duration * v2;
                int segmentCount = (int)(segmentLength == 0 ? 0 : duration / segmentLength) + 1;

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

                    CreateSegment(start, end, trace.timingGroup, trace.timing + (int)(i * segmentLength));
                }

                start = end;
                end = new float3(
                    Conversion.GetWorldX(trace.endX),
                    Conversion.GetWorldY(trace.endY),
                    Conductor.Instance.GetFloorPositionFromTiming(trace.endTiming, trace.timingGroup)
                );

                CreateSegment(start, end, trace.timingGroup, (int)(trace.endTiming - segmentLength));
            }
        }

        private void CreateSegment(float3 start, float3 end, int timingGroup, int time)
        {
            Entity traceEntity = EManager.Instantiate(traceNoteEntityPrefab);
            EManager.SetSharedComponentData<RenderMesh>(traceEntity, new RenderMesh()
            {
                mesh = traceMesh,
                material = traceMaterial
            });
            EManager.SetComponentData<FloorPosition>(traceEntity, new FloorPosition()
            {
                value = start.z
            });

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

            //Shear along xy + scale along z matrix
            EManager.SetComponentData<LocalToWorld>(traceEntity, new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, dy, start.y,
                    0, 0, dz, 0,
                    0, 0, 0,  1
                )
            });

            EManager.SetComponentData<TimingGroup>(traceEntity, new TimingGroup()
            {
                value = timingGroup
            });

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, 0);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, 0);
            int appearTime = (t1 < t2) ? t1 : t2;
            int disappearTime = (t1 < t2) ? t2 : t1;

            EManager.SetComponentData<AppearTime>(traceEntity, new AppearTime()
            {
                value = appearTime
            });
            EManager.SetComponentData<DisappearTime>(traceEntity, new DisappearTime()
            {
                value = disappearTime
            });
            EManager.SetComponentData<ChartTime>(traceEntity, new ChartTime()
            {
                value = time
            });
        }

        private void CreateHeadSegment(AffTrace trace)
        {
            Entity headEntity = EManager.Instantiate(headTraceNoteEntityPrefab);
            EManager.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = traceMaterial
            });
            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(trace.timing, trace.timingGroup);
            EManager.SetComponentData<FloorPosition>(headEntity, new FloorPosition()
            {
                value = floorpos
            });

            float x = Conversion.GetWorldX(trace.startX); 
            float y = Conversion.GetWorldY(trace.startY); 
            const float z = 0;
            EManager.SetComponentData<Translation>(headEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            EManager.SetComponentData<TimingGroup>(headEntity, new TimingGroup()
            {
                value = trace.timingGroup
            });
            
            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, 0);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, 0);
            int appearTime = (t1 < t2) ? t1 : t2;

            EManager.SetComponentData<AppearTime>(headEntity, new AppearTime()
            {
                value = appearTime
            });

            EManager.SetComponentData<ChartTime>(headEntity, new ChartTime()
            {
                value = trace.timing
            });
        }
    }
}