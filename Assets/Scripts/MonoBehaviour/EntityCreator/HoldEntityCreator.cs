using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Data;

namespace ArcCore.MonoBehaviours.EntityCreation
{
    public class HoldEntityCreator : MonoBehaviour
    {
        public static HoldEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject holdNotePrefab;
        private Entity holdNoteEntityPrefab;
        private World defaultWorld;
        private EntityManager entityManager;
        public EntityArchetype holdJudgeArchetype { get; private set; }
        private void Awake()
        {
            Instance = this;
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
            holdNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(holdNotePrefab, settings);
            entityManager.AddComponent<TimingGroup>(holdNoteEntityPrefab); //TEMPORARY TO FIX BUGFCKERY

            holdJudgeArchetype = entityManager.CreateArchetype(
                ComponentType.ReadOnly<ChartTime>(),
                ComponentType.ReadOnly<Track>(),
                ComponentType.ReadOnly<EntityReference>(),
                ComponentType.ReadOnly<Tags.JudgeHoldPoint>()
                );
        }

        public void CreateEntities(List<AffHold> affHoldList)
        {
            affHoldList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (AffHold hold in affHoldList)
            {
                Entity holdEntity = entityManager.Instantiate(holdNoteEntityPrefab);

                float x = Convert.TrackToX(hold.track);
                const float y = 0;
                const float z = 0;

                const float scalex = 1;
                const float scaley = 1;
                float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
                float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
                float scalez = - endFloorPosition + startFloorPosition;

                entityManager.SetComponentData<Translation>(holdEntity, new Translation(){
                    Value = new float3(x, y, z)
                });
                entityManager.AddComponentData<NonUniformScale>(holdEntity, new NonUniformScale(){
                    Value = new float3(scalex, scaley, scalez)
                });
                entityManager.SetComponentData<FloorPosition>(holdEntity, new FloorPosition(){
                    Value = startFloorPosition
                });
                entityManager.SetComponentData<TimingGroup>(holdEntity, new TimingGroup(){
                    Value = hold.timingGroup
                });

                float time = hold.timing;
                TimingEvent timingEvent = Conductor.Instance.GetTimingEventFromTiming(hold.timing, hold.timingGroup);

                while (time < hold.endTiming)
                {
                    time += (timingEvent.bpm >= 255 ? 60_000f : 30_000f) / timingEvent.bpm;

                    Entity judgeEntity = entityManager.CreateEntity(holdJudgeArchetype);
                    entityManager.SetComponentData<ChartTime>(judgeEntity, new ChartTime()
                    {
                        Value = (int)time
                    });
                    entityManager.SetComponentData<Track>(judgeEntity, new Track()
                    {
                        Value = hold.track
                    });
                    entityManager.SetComponentData<EntityReference>(judgeEntity, new EntityReference()
                    {
                        Value = holdEntity
                    });
                    entityManager.SetComponentData<Tags.JudgeHoldPoint>(judgeEntity, new Tags.JudgeHoldPoint());

                    ScoreManager.Instance.maxCombo++;
                }
            }
        }
    }

}