using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Arcaoid.Utility;

public class HoldEntityCreator : MonoBehaviour
{
    public static HoldEntityCreator Instance { get; private set; }
    [SerializeField] private GameObject holdNotePrefab;
    private Entity holdNoteEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        holdNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(holdNotePrefab, settings);
    }

    public void CreateEntities(List<AffHold> affHoldList)
    {
        affHoldList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

        foreach(AffHold hold in affHoldList)
        {
            Entity holdEntity = entityManager.Instantiate(holdNoteEntityPrefab);

            float x = Convert.TrackToX(hold.track);
            const float y = 0;
            const float z = 0;

            const float scalex = 1;
            const float scaley = 1;
            float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
            float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
            float scalez = endFloorPosition - startFloorPosition;

            entityManager.SetComponentData<Translation>(holdEntity, new Translation(){
                Value = new float3(x, y, z)
            });
            entityManager.AddComponentData<NonUniformScale>(holdEntity, new NonUniformScale(){
                Value = new float3(scalex, scaley, scalez)
            });
            entityManager.SetComponentData<FloorPosition>(holdEntity, new FloorPosition(){
                Value = startFloorPosition
            });
        }
    }
}
