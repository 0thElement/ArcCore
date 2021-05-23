using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Components;
using ArcCore.Parsing;
using ArcCore.Components.Chunk;
using static ArcCore.EntityManagement;

namespace ArcCore.Behaviours.EntityCreation
{
    public class HoldEntityCreator : MonoBehaviour
    {
        public static HoldEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject holdNotePrefab;
        private Entity holdNoteEntityPrefab;
        private void Awake()
        {
            Instance = this;
            holdNoteEntityPrefab = GameObjectToNote(holdNotePrefab);
        }

        public unsafe void CreateEntities(List<AffHold> affHoldList)
        {
            affHoldList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (AffHold hold in affHoldList)
            {
                //Main entity
                Entity holdEntity = EManager.Instantiate(holdNoteEntityPrefab);

                float x = Conversion.TrackToX(hold.track);
                const float y = 0;
                const float z = 0;

                const float scalex = 1;
                const float scaley = 1;

                float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
                float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
                float scalez = - endFloorPosition + startFloorPosition;

                EManager.SetComponentData<Translation>(holdEntity, new Translation(){
                    Value = new float3(x, y, z)
                });
                EManager.AddComponentData<NonUniformScale>(holdEntity, new NonUniformScale(){
                    Value = new float3(scalex, scaley, scalez)
                });

                EManager.SetComponentData<FloorPosition>(holdEntity, new FloorPosition(startFloorPosition));
                EManager.SetComponentData<TimingGroup>(holdEntity, new TimingGroup(hold.timingGroup));
                EManager.SetComponentData<ChartTime >(holdEntity, new ChartTime{value = hold.timing});
                /*entityManager.SetComponentData<ShaderCutoff>(holdEntity, new ShaderCutoff()
                {
                    Value = 1f
                });*/

                EManager.SetComponentData(holdEntity, new ChartLane(hold.track));

                //Appear and disappear time
                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(startFloorPosition + Constants.RenderFloorPositionRange, 0);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(endFloorPosition - Constants.RenderFloorPositionRange, 0);
                int appearTime = (t1 < t2) ? t1 : t2;
                int disappearTime = (t1 < t2) ? t2 : t1;

                EManager.SetComponentData<AppearTime>(holdEntity, new AppearTime(appearTime));
                EManager.SetComponentData<DisappearTime>(holdEntity, new DisappearTime(disappearTime));

                //Judge entities
                float startBpm = Conductor.Instance.GetTimingEventFromTiming(hold.timing, hold.timingGroup).bpm;
                EManager.SetComponentData(holdEntity, ChartIncrTime.FromBpm(hold.timing, hold.endTiming, startBpm, out int comboCount));

                //Add combo
                ScoreManager.Instance.maxCombo += comboCount;
            }
        }
    }

}