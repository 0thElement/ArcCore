using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
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
        affTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

        foreach (AffTap tap in affTapList)
        {
            Entity tapEntity = entityManager.Instantiate(tapNoteEntityPrefab);

            float x = Convert.TrackToX(tap.track);
            const float y = 0;
            const float z = 0;
            entityManager.SetComponentData<Translation>(tapEntity, new Translation(){ 
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<FloorPosition>(tapEntity, new FloorPosition(){
                Value = Conductor.Instance.GetFloorPositionFromTiming(tap.timing, tap.timingGroup)
            });
        }
    }
}
