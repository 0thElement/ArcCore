using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Arcaoid.Utility;

public class ArcTapEntityCreator : MonoBehaviour
{
    public static ArcTapEntityCreator Instance { get; private set; }
    [SerializeField] private GameObject arcTapNotePrefab;
    [SerializeField] private GameObject connectionLinePrefab;
    private Entity arcTapNoteEntityPrefab;
    private Entity connectionLineEntityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;
    private void Awake()
    {
        Instance = this;
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        arcTapNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcTapNotePrefab, settings);
        connectionLineEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(connectionLinePrefab, settings);
    }

    public void CreateEntities(List<AffArcTap> affArcTapList, List<AffTap> affTapList)
    {
        affArcTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
        affTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
        int lowBound=0;

        foreach (AffArcTap arctap in affArcTapList)
        {
            Entity tapEntity = entityManager.Instantiate(arcTapNoteEntityPrefab);

            float x = Convert.GetWorldX(arctap.position.x);
            float y = Convert.GetWorldY(arctap.position.y) - 0.5f;
            const float z = 0;
            entityManager.SetComponentData<Translation>(tapEntity, new Translation(){ 
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<FloorPosition>(tapEntity, new FloorPosition(){
                Value = Conductor.Instance.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup)
            });

            while (arctap.timing > affTapList[lowBound].timing)
            {
                lowBound++;
            }
            int highBound=lowBound;
            while (arctap.timing == affTapList[highBound].timing)
            {
                highBound++;
            }
            //if lowbound's timing is greater than arctap's timing, that means there are no tap with the same timing
            //Range from lowbound to highbound are all taps with the same timing

            for (int i=lowBound; i<highBound; i++)
            {
                if (arctap.timingGroup == affTapList[i].timingGroup)
                    CreateConnections(arctap, affTapList[i]);
            }
        }
    }

    public void CreateConnections(AffArcTap arctap, AffTap tap)
    {
        Entity lineEntity = entityManager.Instantiate(connectionLineEntityPrefab);

        float x = Convert.GetWorldX(arctap.position.x);
        float y = Convert.GetWorldX(arctap.position.y);
        const float z = 0;

        float dx = Convert.GetWorldX(Convert.TrackToX(tap.track) - arctap.position.x);
        float dy = Convert.GetWorldY(arctap.position.y);

        float3 direction = new float3(dx, dy, 0);
        float length = math.sqrt(dx*dx + dy*dy);

        entityManager.SetComponentData<Translation>(lineEntity, new Translation(){
            Value = new float3(x, y, z)
        });

        entityManager.SetComponentData<NonUniformScale>(lineEntity, new NonUniformScale(){
            Value = new float3(0.1f, length, 1f)
        });
        
        entityManager.SetComponentData<Rotation>(lineEntity, new Rotation(){
            Value = quaternion.LookRotationSafe(direction, Vector3.up)
        });
    }
}