using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Parsing.Aff;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Behaviours.EntityCreation
{
    public class ArcTapEntityCreator : ECSMonoBehaviour
    {
        public static ArcTapEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject arcTapNotePrefab;
        [SerializeField] private GameObject connectionLinePrefab;
        [SerializeField] private GameObject shadowPrefab;
        private Entity arcTapNoteEntityPrefab;
        private Entity connectionLineEntityPrefab;
        private Entity shadowEntityPrefab;

        private void Awake()
        {
            Instance = this;

            arcTapNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(arcTapNotePrefab, EntityManager);
            connectionLineEntityPrefab = GameObjectConversionSettings.ConvertToNote(connectionLinePrefab, EntityManager);
            shadowEntityPrefab = GameObjectConversionSettings.ConvertToNote(shadowPrefab, EntityManager);
        }

        public void CreateEntities(List<AffArcTap> affArcTapList, List<AffTap> affTapList)
        {
            affArcTapList.Sort((item1, item2) => { return item1.Timing.CompareTo(item2.Timing); });
            affTapList.Sort((item1, item2) => { return item1.Timing.CompareTo(item2.Timing); });
            int lowBound=0;

            foreach (AffArcTap arctap in affArcTapList)
            {
                //Main entity
                Entity tapEntity = EntityManager.Instantiate(arcTapNoteEntityPrefab);
                Entity shadowEntity = EntityManager.Instantiate(shadowEntityPrefab);

                float x = Conversion.GetWorldX(arctap.position.x);
                float y = Conversion.GetWorldY(arctap.position.y);
                const float z = 0;

                EntityManager.SetComponentData<Translation>(tapEntity, new Translation()
                {
                    Value = new float3(x, y, z)
                });
                EntityManager.SetComponentData<Translation>(shadowEntity, new Translation()
                {
                    Value = new float3(x, 0, z)
                });

                float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arctap.Timing, arctap.timingGroup);
                FloorPosition floorPositionF = new FloorPosition()
                {
                    value = floorpos
                };

                EntityManager.SetComponentData<FloorPosition>(tapEntity, floorPositionF);
                EntityManager.SetComponentData<FloorPosition>(shadowEntity, floorPositionF);

                TimingGroup group = new TimingGroup()
                {
                    value = arctap.timingGroup
                };

                EntityManager.SetComponentData<TimingGroup>(tapEntity, group);
                EntityManager.SetComponentData<TimingGroup>(shadowEntity, group);

                int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arctap.timingGroup);
                int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arctap.timingGroup);
                int appearTime = (t1 < t2) ? t1 : t2;

                EntityManager.SetComponentData<AppearTime>(tapEntity, new AppearTime(){ value = appearTime });

                //Judge entities
                EntityManager.SetComponentData(tapEntity, new ChartTime(arctap.Timing));
                EntityManager.SetComponentData(tapEntity, new ChartPosition(Conversion.GetWorldPos(arctap.position)));

                //Connection line
                while (lowBound < affTapList.Count && arctap.Timing > affTapList[lowBound].Timing)
                {
                    lowBound++;
                }
                //Iterated the whole list without finding anything
                if (lowBound >= affTapList.Count) continue;

                int highBound = lowBound;
                while (highBound < affTapList.Count && arctap.Timing == affTapList[highBound].Timing)
                {
                    highBound++;
                }
                //if lowbound's timing is greater than arctap's timing, that means there are no tap with the same timing
                //Range from lowbound to highbound are all taps with the same timing

                for (int j=lowBound; j<highBound; j++)
                {
                    if (arctap.timingGroup == affTapList[j].timingGroup)
                        CreateConnections(arctap, affTapList[j], appearTime);
                }

                EntityManager.SetComponentData<EntityReference>(tapEntity, new EntityReference()
                {
                    value = shadowEntity
                });
            }

        }

        public void CreateConnections(AffArcTap arctap, AffTap tap, int appearTime)
        {
            Entity lineEntity = EntityManager.Instantiate(connectionLineEntityPrefab);

            float x = Conversion.GetWorldX(arctap.position.x);
            float y = Conversion.GetWorldY(arctap.position.y) - 0.5f;
            const float z = 0;

            float dx = Conversion.TrackToX(tap.track) - x;
            float dy = -y;

            float3 direction = new float3(dx, dy, 0);
            float length = math.sqrt(dx*dx + dy*dy);

            EntityManager.SetComponentData<Translation>(lineEntity, new Translation(){
                Value = new float3(x, y, z)
            });

            EntityManager.AddComponentData<NonUniformScale>(lineEntity, new NonUniformScale(){
                Value = new float3(1f, 1f, length)
            });
            
            EntityManager.SetComponentData<Rotation>(lineEntity, new Rotation(){
                Value = quaternion.LookRotationSafe(direction, new Vector3(0,0,1))
            });

            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arctap.Timing, arctap.timingGroup);
            EntityManager.AddComponentData<FloorPosition>(lineEntity, new FloorPosition(){
                value = floorpos
            });
            EntityManager.SetComponentData<TimingGroup>(lineEntity, new TimingGroup()
            {
                value = arctap.timingGroup
            });
            EntityManager.SetComponentData<AppearTime>(lineEntity, new AppearTime(){
                value = appearTime
            });
            EntityManager.SetComponentData<ChartTime>(lineEntity, new ChartTime(){
                value = arctap.timing
            });
        }
    }
}