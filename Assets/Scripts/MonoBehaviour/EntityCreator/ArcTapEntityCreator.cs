using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Arcaoid.Utility;

public class ArcTapEntityCreator : MonoBehaviour
{
    public static ArcTapEntityCreator Instance { get; private set; }
    [SerializeField] private GameObject arcTapNotePrefab;
    private Entity arcTapNoteEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        arcTapNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcTapNotePrefab, settings);
    }

    public void CreateEntities(List<AffArcTap> affArcTapList)
    {
        foreach (AffArcTap arctap in affArcTapList)
        {
            Entity tapEntity = entityManager.Instantiate(arcTapNoteEntityPrefab);

            float x = Convert.GetWorldX(arctap.position.x);
            float y = Convert.GetWorldY(arctap.position.y) - 0.5f;
            if (y>5)
                Debug.Log(arctap.timing + ": " + arctap.position.x + " " + arctap.position.y + " -> " + x + " " + y);
            const float z = 0;
            entityManager.SetComponentData<Translation>(tapEntity, new Translation(){ 
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<FloorPosition>(tapEntity, new FloorPosition(){
                Value = Conductor.Instance.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup)
            });
        }
    }
}
