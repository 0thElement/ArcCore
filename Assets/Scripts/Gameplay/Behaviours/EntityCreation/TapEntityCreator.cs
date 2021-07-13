using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Components;
using Unity.Collections;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Parsing.Data;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Behaviours.EntityCreation
{

    public class TapEntityCreator : ECSMonoBehaviour
    {
        //TODO: MODIFY PREFAB (sorry 0th, i didnt have the editor open while combining the entities lol

        public static TapEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject tapNotePrefab;
        private Entity tapNoteEntityPrefab;
        private void Awake()
        {
            Instance = this;
            tapNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(tapNotePrefab, EntityManager);
        }

        public void CreateEntities(List<TapRaw> affTapList)
        {
            affTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (TapRaw tap in affTapList)
            {
                //Main Entity
                Entity tapEntity = EntityManager.Instantiate(tapNoteEntityPrefab);

                float x = Conversion.TrackToX(tap.track);
                const float y = 0;
                const float z = 0;

                EntityManager.SetComponentData(tapEntity, new Translation(){ 
                    Value = new float3(x, y, z)
                });

                float floorpos = Conductor.Instance.GetFloorPositionFromTiming(tap.timing, tap.timingGroup);
                EntityManager.SetComponentData(tapEntity, new FloorPosition(){
                    value = floorpos 
                });
                EntityManager.SetComponentData(tapEntity, new TimingGroup()
                {
                    value = tap.timingGroup
                });

                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, tap.timingGroup);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, tap.timingGroup);
                int appearTime = (t1 < t2) ? t1 : t2;

                EntityManager.SetComponentData(tapEntity, new AppearTime(){ value = appearTime });
                
                EntityManager.SetComponentData(tapEntity, new ChartTime(tap.timing));
                EntityManager.SetComponentData(tapEntity, new ChartLane(tap.track));

                ScoreManager.Instance.tracker.noteCount++;
            }
        }
    }

}