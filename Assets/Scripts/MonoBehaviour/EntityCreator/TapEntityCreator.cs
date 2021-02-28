using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Arcaoid.Utility;

public class TapEntityCreator : MonoBehaviour
{
    public static TapEntityCreator Instance { get; private set; }
    [SerializeField] private GameObject tapNotePrefab;
    private Entity tapNoteEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        tapNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(tapNotePrefab, settings);
    }

    public void CreateEntities(List<AffTap> affTapList)
    {
        foreach (AffTap tap in affTapList)
        {
            Entity tapEntity = entityManager.Instantiate(tapNoteEntityPrefab);

            float x = Convert.TrackToX(tap.track);
            float y = 0;
            float z = -200;
            entityManager.SetComponentData<Translation>(tapEntity, new Translation{ 
                Value = new float3(x, y, z)
            });
        }
    }
}
