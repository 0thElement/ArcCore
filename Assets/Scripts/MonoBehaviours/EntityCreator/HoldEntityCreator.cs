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
            foreach (AffHold hold in affHoldList)
            {
                Entity holdEntity = entityManager.Instantiate(holdNoteEntityPrefab);

                float x = Convert.TrackToX(hold.track);
                const float y = 0;
                const float z = 0;

                const float scalex = 1.53f;
                float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
                float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
                float scaley = (endFloorPosition - startFloorPosition) / 3790f;
                const float scalez = 1;

                entityManager.SetComponentData<Translation>(holdEntity, new Translation()
                {
                    Value = new float3(x, y, z)
                });
                entityManager.SetComponentData<NonUniformScale>(holdEntity, new NonUniformScale()
                {
                    Value = new float3(scalex, scaley, scalez)
                });
                entityManager.SetComponentData<FloorPosition>(holdEntity, new FloorPosition()
                {
                    value = startFloorPosition
                });

                float time = hold.timing;
                TimingEvent timingEvent = Conductor.Instance.GetTimingEventFromTiming(hold.timing, hold.timingGroup);

                while (time < hold.endTiming)
                {
                    time += (timingEvent.bpm >= 255 ? 60_000f : 30_000f) / timingEvent.bpm;

                    Entity judgeEntity = entityManager.CreateEntity(typeof(JudgeTime), typeof(JudgeLane), typeof(Tags.JudgeHold));
                    entityManager.SetComponentData<JudgeTime>(judgeEntity, new JudgeTime()
                    {
                        time = (int)time
                    });
                    entityManager.SetComponentData<JudgeLane>(judgeEntity, new JudgeLane()
                    {
                        lane = hold.track
                    });

                    ScoreManager.Instance.maxCombo++;
                }
            }
        }
    }
}
