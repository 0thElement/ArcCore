using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Arcaoid.Utility;

public class TraceEntityCreator : MonoBehaviour
{
    public static TraceEntityCreator Instance { get; private set; }
    [SerializeField] private GameObject traceNotePrefab;
    [SerializeField] private GameObject headTraceNotePrefab;
    [SerializeField] private Material traceMaterial;
    private Entity traceNoteEntityPrefab;
    private Entity headTraceNoteEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private Mesh traceMesh;
    private Mesh headMesh;
    private int colorShaderId;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        traceNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(traceNotePrefab, settings);
        traceMesh = CreateBaseTraceSegmentMesh();
        //Remove these component to allow direct access to localtoworld matrices
        //idk if this is a good way to set up an entity prefab in this case but this will do for now
        entityManager.RemoveComponent<Translation>(traceNoteEntityPrefab);
        entityManager.RemoveComponent<Rotation>(traceNoteEntityPrefab);

        headMesh = CreateBaseHeadTraceSegmentMesh();
        headTraceNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(headTraceNotePrefab, settings);

        colorShaderId = Shader.PropertyToID("_Color");
    }
    //Keeping this separate in the event i want trace's shape to be different
    public Mesh CreateBaseTraceSegmentMesh()
    {
        Vector3 toPos = new float3(0, 0, -1);
        float offset = 0.15f;

        Vector3[] vertices = new Vector3[6];
        Vector2[] uv = new Vector2[6];
        int[] triangles = new int[] { 0, 3, 2, 0, 2, 1, 0, 5, 4, 0, 4, 1 };

        vertices[0] = new Vector3(0, offset / 2, 0);
        uv[0] = new Vector2();
        vertices[1] = new Vector3(0, offset / 2, -1);
        uv[1] = new Vector2(0, 1);
        vertices[2] = new Vector3(offset, -offset / 2, -1);
        uv[2] = new Vector2(1, 1);
        vertices[3] = new Vector3(offset, -offset / 2, 0);
        uv[3] = new Vector2(1, 0);
        vertices[4] = new Vector3(-offset, -offset / 2, -1);
        uv[4] = new Vector2(1, 1);
        vertices[5] = new Vector3(-offset, -offset / 2, 0);
        uv[5] = new Vector2(1, 0);

        return new Mesh(){
            vertices = vertices,
            uv = uv,
            triangles = triangles
        };
    }

    public Mesh CreateBaseHeadTraceSegmentMesh()
    {
        float offset = 0.15f;

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };

        vertices[0] = new Vector3(0, offset / 2, 0);
        uv[0] = new Vector2();
        vertices[1] = new Vector3(offset, -offset / 2, 0);
        uv[1] = new Vector2(1, 0);
        vertices[2] = new Vector3(0, -offset / 2, offset / 2);
        uv[2] = new Vector2(1, 1);
        vertices[3] = new Vector3(-offset, -offset / 2, 0);
        uv[3] = new Vector2(1, 1);

        return new Mesh(){
            vertices = vertices,
            uv = uv,
            triangles = triangles
        };
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

                CreateSegment(start, end);
            }

            start = end;
            end = new float3(
                Convert.GetWorldX(trace.endX),
                Convert.GetWorldY(trace.endY),
                Conductor.Instance.GetFloorPositionFromTiming(trace.endTiming, trace.timingGroup)
            );

            CreateSegment(start, end);
        }
    }

    private void CreateSegment(float3 start, float3 end)
    {
        Entity traceEntity = entityManager.Instantiate(traceNoteEntityPrefab);
        entityManager.SetSharedComponentData<RenderMesh>(traceEntity, new RenderMesh()
        {
            mesh = traceMesh,
            material = traceMaterial
        });
        entityManager.SetComponentData<StartEndPosition>(traceEntity, new StartEndPosition()
        {
            StartPosition = start,
            EndPosition = end
        });
        entityManager.SetComponentData<FloorPosition>(traceEntity, new FloorPosition()
        {
            Value = start.z
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
    }
}