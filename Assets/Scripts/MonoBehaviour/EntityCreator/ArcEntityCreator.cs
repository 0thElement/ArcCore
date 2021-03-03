﻿using System.Collections.Generic;
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
    [SerializeField] private GameObject headArcNotePrefab;
    [SerializeField] private GameObject heightIndicatorPrefab;
    [SerializeField] private Material arcMaterial;
    [SerializeField] private Material heightMaterial;
    [SerializeField] private Color[] arcColors;
    [SerializeField] private Mesh arcMesh;
    [SerializeField] private Mesh headMesh;
    private Entity arcNoteEntityPrefab;
    private Entity headArcNoteEntityPrefab;
    private Entity heightIndicatorEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private int colorShaderId;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        arcNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcNotePrefab, settings);
        //Remove these component to allow direct access to localtoworld matrices
        //idk if this is a good way to set up an entity prefab in this case but this will do for now
        entityManager.RemoveComponent<Translation>(arcNoteEntityPrefab);
        entityManager.RemoveComponent<Rotation>(arcNoteEntityPrefab);

        headArcNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(headArcNotePrefab, settings);

        heightIndicatorEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(heightIndicatorPrefab, settings);

        colorShaderId = Shader.PropertyToID("_Color");
    }

    public void CreateEntities(List<List<AffArc>> affArcList)
    {
        int colorId=0;
        foreach (List<AffArc> listByColor in affArcList)
        {
            listByColor.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            Material arcColorMaterialInstance = Instantiate(arcMaterial);
            Material heightIndicatorColorMaterialInstance = Instantiate(heightMaterial);
            arcColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);
            heightIndicatorColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);
            colorId++;

            List<float4> connectedArcsIdEndpoint = new List<float4>();

            foreach (AffArc arc in listByColor)
            {
                //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
                //this is really dumb but i don't want to split this into another class
                float4 arcStartPoint = new float4((float)arc.timingGroup, (float)arc.timing, arc.startX, arc.startY);
                float4 arcEndPoint = new float4((float)arc.timingGroup, (float)arc.endTiming, arc.endX, arc.endY);
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
                    arcId = connectedArcsIdEndpoint.Count;
                    connectedArcsIdEndpoint.Add(arcEndPoint);
                    CreateHeadSegment(arc, arcColorMaterialInstance);
                }
                if (isHeadArc || arc.startY != arc.endY)
                    CreateHeightIndicator(arc, heightIndicatorColorMaterialInstance);

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

    private void CreateHeightIndicator(AffArc arc, Material material)
    {
        Entity heightEntity = entityManager.Instantiate(heightIndicatorEntityPrefab);

        float height = Convert.GetWorldY(arc.startY) - 0.45f;

        float x = Convert.GetWorldX(arc.startX); 
        float y = height / 2;
        const float z = 0;

        const float scaleX = 2.34f;
        float scaleY = height;
        const float scaleZ = 1;

        Mesh mesh = entityManager.GetSharedComponentData<RenderMesh>(heightEntity).mesh; 
        entityManager.SetSharedComponentData<RenderMesh>(heightEntity, new RenderMesh()
        {
            mesh = mesh,
            material = material 
        });

        entityManager.SetComponentData<Translation>(heightEntity, new Translation()
        {
            Value = new float3(x, y, z)
        });
        entityManager.AddComponentData<NonUniformScale>(heightEntity, new NonUniformScale()
        {
            Value = new float3(scaleX, scaleY, scaleZ)
        });
    }

    private void CreateHeadSegment(AffArc arc, Material material)
    {
        Entity headEntity = entityManager.Instantiate(headArcNoteEntityPrefab);
        entityManager.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
            mesh = headMesh,
            material = material
        });
        entityManager.SetComponentData<FloorPosition>(headEntity, new FloorPosition()
        {
            Value = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
        });

        float x = Convert.GetWorldX(arc.startX); 
        float y = Convert.GetWorldY(arc.startY); 
        const float z = 0;
        entityManager.SetComponentData<Translation>(headEntity, new Translation()
        {
            Value = new float3(x, y, z)
        });
    }
}