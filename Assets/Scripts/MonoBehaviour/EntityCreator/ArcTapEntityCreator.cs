using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Data;

namespace ArcCore.MonoBehaviours.EntityCreation
{

    public class ArcTapEntityCreator : MonoBehaviour
    {
        public static ArcTapEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject arcTapNotePrefab;
        [SerializeField] private GameObject connectionLinePrefab;
        private Entity arcTapNoteEntityPrefab;
        private Entity connectionLineEntityPrefab;
        private World defaultWorld;
        private EntityManager entityManager;
        public EntityArchetype arctapJudgeArchetype { get; private set; }
        private void Awake()
        {
            Instance = this;
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
            arcTapNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcTapNotePrefab, settings);
            connectionLineEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(connectionLinePrefab, settings);

            arctapJudgeArchetype = entityManager.CreateArchetype(
                ComponentType.ReadOnly<ChartTime>(),
                ComponentType.ReadOnly<SinglePosition>(),
                ComponentType.ReadOnly<EntityReference>()
                );
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
                float y = Convert.GetWorldY(arctap.position.y);
                const float z = 0;
                entityManager.SetComponentData<Translation>(tapEntity, new Translation(){ 
                    Value = new float3(x, y, z)
                });
                entityManager.SetComponentData<FloorPosition>(tapEntity, new FloorPosition(){
                    Value = Conductor.Instance.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup)
                });
                entityManager.SetComponentData<TimingGroup>(tapEntity, new TimingGroup()
                {
                    Value = arctap.timingGroup
                });

                //Connection line
                while (lowBound < affTapList.Count && arctap.timing > affTapList[lowBound].timing)
                {
                    lowBound++;
                }
                //Iterated the whole list without finding anything
                if (lowBound >= affTapList.Count) continue;

                int highBound = lowBound;
                while (highBound < affTapList.Count && arctap.timing == affTapList[highBound].timing)
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
            float y = Convert.GetWorldY(arctap.position.y) - 0.5f;
            const float z = 0;

            float dx = Convert.TrackToX(tap.track) - x;
            float dy = -y;

            float3 direction = new float3(dx, dy, 0);
            float length = math.sqrt(dx*dx + dy*dy);

            entityManager.SetComponentData<Translation>(lineEntity, new Translation(){
                Value = new float3(x, y, z)
            });

            entityManager.AddComponentData<NonUniformScale>(lineEntity, new NonUniformScale(){
                Value = new float3(1f, 1f, length)
            });
            
            entityManager.SetComponentData<Rotation>(lineEntity, new Rotation(){
                Value = quaternion.LookRotationSafe(direction, new Vector3(0,0,1))
            });
            entityManager.AddComponentData<FloorPosition>(lineEntity, new FloorPosition(){
                Value = Conductor.Instance.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup)
            });
            entityManager.SetComponentData<TimingGroup>(lineEntity, new TimingGroup()
            {
                Value = arctap.timingGroup
            });
        }

        public void CreateJudgeEntity(AffArcTap arctap, Entity arctapEntity)
        {
            Entity judgeEntity = entityManager.CreateEntity(arctapJudgeArchetype);

            entityManager.SetComponentData<ChartTime>(judgeEntity, new ChartTime()
            {
                Value = arctap.timing
            });
            entityManager.SetComponentData<SinglePosition>(judgeEntity, new SinglePosition()
            {
                Value = Convert.GetWorldPos(arctap.position)
            });
            entityManager.SetComponentData<EntityReference>(judgeEntity, new EntityReference()
            {
                Value = arctapEntity
            });
        }
    }
}