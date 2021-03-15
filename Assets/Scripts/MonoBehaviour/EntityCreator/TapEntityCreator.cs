using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Data;
using Unity.Collections;

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
            entityManager.AddComponent<Disabled>(tapNoteEntityPrefab);
            entityManager.AddChunkComponentData<ChunkAppearTime>(tapNoteEntityPrefab);

            tapJudgeArchetype = entityManager.CreateArchetype(
                ComponentType.ReadOnly<ChartTime>(),
                ComponentType.ReadOnly<Track>(),
                ComponentType.ReadOnly<ArctapFunnelPtr>()
                );
        }

        public unsafe void CreateEntities(List<AffTap> affTapList)
        {
            affTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (AffTap tap in affTapList)
            {
                //Main Entity
                Entity tapEntity = entityManager.Instantiate(tapNoteEntityPrefab);

                float x = Convert.TrackToX(tap.track);
                const float y = 0;
                const float z = 0;

                ArctapFunnel* tapFunnelPtr = CreateTapFunnelPtr();

                entityManager.SetComponentData<Translation>(tapEntity, new Translation(){ 
                    Value = new float3(x, y, z)
                });

                float floorpos = Conductor.Instance.GetFloorPositionFromTiming(tap.timing, tap.timingGroup);
                entityManager.SetComponentData<FloorPosition>(tapEntity, new FloorPosition(){
                    Value = floorpos 
                });
                entityManager.SetComponentData<TimingGroup>(tapEntity, new TimingGroup()
                {
                    Value = tap.timingGroup
                });
                entityManager.SetComponentData<ArctapFunnelPtr>(tapEntity, new ArctapFunnelPtr()
                {
                    Value = tapFunnelPtr
                });

                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, tap.timingGroup);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, tap.timingGroup);
                int appearTime = (t1 < t2) ? t1 : t2;

                entityManager.SetComponentData<AppearTime>(tapEntity, new AppearTime(){ Value = appearTime });

                //Judge component
                Entity judgeEntity = entityManager.CreateEntity(tapJudgeArchetype);
                
                entityManager.SetComponentData<ChartTime>(judgeEntity, new ChartTime()
                {
                    Value = tap.timing
                });;
                entityManager.SetComponentData<Track>(judgeEntity, new Track()
                {
                    Value = tap.track
                });
                entityManager.SetComponentData<ArctapFunnelPtr>(judgeEntity, new ArctapFunnelPtr()
                {
                    Value = tapFunnelPtr
                });

                ScoreManager.Instance.maxCombo++;
            }
        }

        private unsafe ArctapFunnel* CreateTapFunnelPtr()
        {
            ArctapFunnel funnel = new ArctapFunnel(true);
            return &funnel;
        }
    }

}