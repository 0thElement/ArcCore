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
        //TODO: MODIFY PREFAB (sorry 0th, i didnt have the editor open while combining the entities lol

        public static TapEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject tapNotePrefab;
        private Entity tapNoteEntityPrefab;
        private void Awake()
        {
            Instance = this;
            tapNoteEntityPrefab = EntityManagement.GameObjectToEntity(tapNotePrefab);

            EntityManager entityManager = EntityManagement.entityManager;

            entityManager.AddComponent<Disabled>(tapNoteEntityPrefab);
            entityManager.AddChunkComponentData<ChunkAppearTime>(tapNoteEntityPrefab);
        }

        public void CreateEntities(List<AffTap> affTapList)
        {
            affTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            EntityManager entityManager = EntityManagement.entityManager;

            foreach (AffTap tap in affTapList)
            {
                //Main Entity
                Entity tapEntity = entityManager.Instantiate(tapNoteEntityPrefab);

                float x = ArccoreConvert.TrackToX(tap.track);
                const float y = 0;
                const float z = 0;

                entityManager.SetComponentData(tapEntity, new Translation(){ 
                    Value = new float3(x, y, z)
                });

                float floorpos = Conductor.Instance.GetFloorPositionFromTiming(tap.timing, tap.timingGroup);
                entityManager.SetComponentData(tapEntity, new FloorPosition(){
                    Value = floorpos 
                });
                entityManager.SetComponentData(tapEntity, new TimingGroup()
                {
                    Value = tap.timingGroup
                });

                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, tap.timingGroup);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, tap.timingGroup);
                int appearTime = (t1 < t2) ? t1 : t2;

                entityManager.SetComponentData(tapEntity, new AppearTime(){ Value = appearTime });
                
                entityManager.SetComponentData(tapEntity, new ChartTime(tap.timing));
                entityManager.SetComponentData(tapEntity, new ChartPosition(tap.track));

                ScoreManager.Instance.maxCombo++;
            }
        }
    }

}