using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Data;


namespace ArcCore.MonoBehaviours.EntityCreation
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
        private World defaultWorld;
        private EntityManager entityManager;
        private int colorShaderId;
        private void Awake()
        {
            Instance = this;
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
            traceNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(traceNotePrefab, settings);
            //Remove these component to allow direct access to localtoworld matrices
            //idk if this is a good way to set up an entity prefab in this case but this will do for now
            entityManager.RemoveComponent<Translation>(traceNoteEntityPrefab);
            entityManager.RemoveComponent<Rotation>(traceNoteEntityPrefab);

            headTraceNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(headTraceNotePrefab, settings);

            colorShaderId = Shader.PropertyToID("_Color");
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
                    Convert.GetWorldX(trace.startX),
                    Convert.GetWorldY(trace.startY),
                    Conductor.Instance.GetFloorPositionFromTiming(trace.timing, trace.timingGroup)
                );

                for (int i=0; i<segmentCount - 1; i++)
                {
                    int t = (int)((i + 1) * segmentLength);
                    start = end;
                    end = new float3(
                        Convert.GetWorldX(Convert.GetXAt((float)t / duration, trace.startX, trace.endX, trace.easing)),
                        Convert.GetWorldY(Convert.GetYAt((float)t / duration, trace.startY, trace.endY, trace.easing)),
                        Conductor.Instance.GetFloorPositionFromTiming(trace.timing + t, trace.timingGroup)
                    );

                    CreateSegment(start, end, trace.timingGroup);
                }

                start = end;
                end = new float3(
                    Convert.GetWorldX(trace.endX),
                    Convert.GetWorldY(trace.endY),
                    Conductor.Instance.GetFloorPositionFromTiming(trace.endTiming, trace.timingGroup)
                );

                CreateSegment(start, end, trace.timingGroup);
            }
        }

        private void CreateSegment(float3 start, float3 end, int timingGroup)
        {
            Entity traceEntity = entityManager.Instantiate(traceNoteEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(traceEntity, new RenderMesh()
            {
                mesh = traceMesh,
                material = traceMaterial
            });
            entityManager.SetComponentData<FloorPosition>(traceEntity, new FloorPosition()
            {
                Value = start.z
            });

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

            //Shear along xy + scale along z matrix
            entityManager.SetComponentData<LocalToWorld>(traceEntity, new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, dy, start.y,
                    0, 0, dz, 0,
                    0, 0, 0,  1
                )
            });

            entityManager.SetComponentData<TimingGroup>(traceEntity, new TimingGroup()
            {
                Value = timingGroup
            });
        }

        private void CreateHeadSegment(AffTrace trace)
        {
            Entity headEntity = entityManager.Instantiate(headTraceNoteEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = traceMaterial
            });
            entityManager.SetComponentData<FloorPosition>(headEntity, new FloorPosition()
            {
                Value = Conductor.Instance.GetFloorPositionFromTiming(trace.timing, trace.timingGroup)
            });

            float x = Convert.GetWorldX(trace.startX); 
            float y = Convert.GetWorldY(trace.startY); 
            const float z = 0;
            entityManager.SetComponentData<Translation>(headEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<TimingGroup>(headEntity, new TimingGroup()
            {
                Value = trace.timingGroup
            });
        }
    }
}