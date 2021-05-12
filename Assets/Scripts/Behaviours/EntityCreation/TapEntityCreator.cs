using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Components;
using Unity.Collections;
using ArcCore.Components.Chunk;
using ArcCore.Parsing;
using static ArcCore.EntityManagement;

namespace ArcCore.Behaviours.EntityCreation
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
            tapNoteEntityPrefab = GameObjectToNote(tapNotePrefab);
        }

        public void CreateEntities(List<AffTap> affTapList)
        {
            affTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (AffTap tap in affTapList)
            {
                //Main Entity
                Entity tapEntity = EManager.Instantiate(tapNoteEntityPrefab);

                float x = Conversion.TrackToX(tap.track);
                const float y = 0;
                const float z = 0;

                EManager.SetComponentData(tapEntity, new Translation(){ 
                    Value = new float3(x, y, z)
                });

                float floorpos = Conductor.Instance.GetFloorPositionFromTiming(tap.timing, tap.timingGroup);
                EManager.SetComponentData(tapEntity, new FloorPosition(){
                    value = floorpos 
                });
                EManager.SetComponentData(tapEntity, new TimingGroup()
                {
                    value = tap.timingGroup
                });

                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, tap.timingGroup);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, tap.timingGroup);
                int appearTime = (t1 < t2) ? t1 : t2;

                EManager.SetComponentData(tapEntity, new AppearTime(){ value = appearTime });
                
                EManager.SetComponentData(tapEntity, new ChartTime(tap.timing));
                EManager.SetComponentData(tapEntity, new ChartLane(tap.track));

                ScoreManager.Instance.maxCombo++;
            }
        }
    }

}