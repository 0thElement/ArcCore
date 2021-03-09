using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Data;
using ArcCore.MonoBehaviours;

namespace ArcCore.MonoBehaviours.EntityCreation
{

    public class TapEntityCreator : MonoBehaviour
    {
        public static TapEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject tapNotePrefab;
        private Entity tapNoteEntityPrefab;
        private World defaultWorld;
        private EntityManager entityManager;
        public EntityArchetype tapJudgeArchetype { get; private set; }
        private void Awake()
        {
            Instance = this;
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
            tapNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(tapNotePrefab, settings);
            entityManager.AddComponent<TimingGroup>(tapNoteEntityPrefab); //TEMPORARY TO FIX BUGFCKERY

            tapJudgeArchetype = entityManager.CreateArchetype(
                ComponentType.ReadOnly<ChartTime>(),
                ComponentType.ReadOnly<Track>(),
                ComponentType.ReadOnly<EntityReference>()
                );
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
                entityManager.SetComponentData<TimingGroup>(tapEntity, new TimingGroup()
                {
                    Value = tap.timingGroup
                });

                Entity judgeEntity = entityManager.CreateEntity(tapJudgeArchetype);
                entityManager.SetComponentData<ChartTime>(judgeEntity, new ChartTime()
                {
                    Value = tap.timing
                });;
                entityManager.SetComponentData<Track>(judgeEntity, new Track()
                {
                    Value = tap.track
                });
                entityManager.SetComponentData<EntityReference>(judgeEntity, new EntityReference()
                {
                    Value = tapEntity
                });

                ScoreManager.Instance.maxCombo++;
            }
        }
    }

}