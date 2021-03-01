using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Arcaoid.Utility;

public class ArcEntityCreator : MonoBehaviour
{
    public static ArcEntityCreator Instance { get; private set; }
    [SerializeField] private GameObject arcNotePrefab;
    [SerializeField] private Material arcMaterial;
    [SerializeField] private Color[] arcColors;
    private Entity arcNoteEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private Mesh arcMesh;
    private int colorShaderId;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        arcNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcNotePrefab, settings);
        arcMesh = CreateBaseArcSegmentMesh();
        //Remove these component to allow direct access to localtoworld matrices
        //idk if this is a good way to set up an entity prefab in this case but this will do for now
        entityManager.RemoveComponent<Translation>(arcNoteEntityPrefab);
        entityManager.RemoveComponent<Rotation>(arcNoteEntityPrefab);
        colorShaderId = Shader.PropertyToID("_Color");
    }
    public Mesh CreateBaseArcSegmentMesh()
    {
        Vector3 fromPos = new float3(0, 0, 0);
        Vector3 toPos = new float3(0, 0, -1);
        float offset = 0.9f;

        Vector3[] vertices = new Vector3[6];
        Vector2[] uv = new Vector2[6];
        int[] triangles = new int[] { 0, 3, 2, 0, 2, 1, 0, 5, 4, 0, 4, 1 };

        vertices[0] = fromPos + new Vector3(0, offset / 2, 0);
        uv[0] = new Vector2();
        vertices[1] = toPos + new Vector3(0, offset / 2, 0);
        uv[1] = new Vector2(0, 1);
        vertices[2] = toPos + new Vector3(offset, -offset / 2, 0);
        uv[2] = new Vector2(1, 1);
        vertices[3] = fromPos + new Vector3(offset, -offset / 2, 0);
        uv[3] = new Vector2(1, 0);
        vertices[4] = toPos + new Vector3(-offset, -offset / 2, 0);
        uv[4] = new Vector2(1, 1);
        vertices[5] = fromPos + new Vector3(-offset, -offset / 2, 0);
        uv[5] = new Vector2(1, 0);

        return new Mesh(){
            vertices = vertices,
            uv = uv,
            triangles = triangles
        };
    }

    public void CreateEntities(List<List<AffArc>> affArcList)
    {
        int colorId=0;
        foreach (List<AffArc> listByColor in affArcList)
        {
            Material arcColorMaterialInstance = Instantiate(arcMaterial);
            arcColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId++]);
            Debug.Log(arcColorMaterialInstance.renderQueue);

            List<float3> connectedArcsIdEndpoint = new List<float3>();

            foreach (AffArc arc in listByColor)
            {
                //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
                float3 arcStartPoint = new float3((float)arc.timing, arc.startX, arc.startY);
                float3 arcEndPoint = new float3((float)arc.endTiming, arc.endX, arc.endY);
                int arcId = -1;
                bool isHeadArc = true;
                for (int id = 0; id < connectedArcsIdEndpoint.Count; id++)
                {
                    if (connectedArcsIdEndpoint[id].Equals(arcStartPoint))
                    {
                        arcId = id;
                        isHeadArc = false;
                        connectedArcsIdEndpoint[id] = arcEndPoint;
                    }
                }

                if (isHeadArc)
                {
                    //todo: create Archead and height indicator
                    arcId = connectedArcsIdEndpoint.Count;
                    connectedArcsIdEndpoint.Add(arcEndPoint);
                }

                //Generate arc segments and shadow segment(each segment is its own entity)
                int duration = arc.endTiming - arc.timing;
                int v1 = duration < 1000 ? 14 : 7;
                float v2 = 1f / (v1 * duration / 1000f);
                float segmentLength = duration * v2;
                int segmentCount = (int)(segmentLength == 0 ? 0 : duration / segmentLength) + 1;

                float3 start;
                float3 end = new float3(
                    Convert.GetWorldX(arc.startX),
                    Convert.GetWorldY(arc.startY),
                    Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                );

                for (int i=0; i<segmentCount - 1; i++)
                {
                    int t = (int)((i + 1) * segmentLength);
                    start = end;
                    end = new float3(
                        Convert.GetWorldX(Convert.GetXAt((float)t / duration, arc.startX, arc.endX, arc.easing)),
                        Convert.GetWorldY(Convert.GetYAt((float)t / duration, arc.startY, arc.endY, arc.easing)),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.timing + t, arc.timingGroup)
                    );

                    CreateSegment(arcColorMaterialInstance, start, end);
                }

                start = end;
                end = new float3(
                    Convert.GetWorldX(arc.endX),
                    Convert.GetWorldY(arc.endY),
                    Conductor.Instance.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                );

                CreateSegment(arcColorMaterialInstance, start, end);
            }
        }
    }

    private void CreateSegment(Material arcColorMaterialInstance, float3 start, float3 end)
    {
        Entity arcEntity = entityManager.Instantiate(arcNoteEntityPrefab);
        entityManager.SetSharedComponentData<RenderMesh>(arcEntity, new RenderMesh()
        {
            mesh = arcMesh,
            material = arcColorMaterialInstance
        });
        entityManager.SetComponentData<StartEndPosition>(arcEntity, new StartEndPosition()
        {
            StartPosition = start,
            EndPosition = end
        });
        entityManager.SetComponentData<FloorPosition>(arcEntity, new FloorPosition()
        {
            Value = start.z
        });
    }
}
