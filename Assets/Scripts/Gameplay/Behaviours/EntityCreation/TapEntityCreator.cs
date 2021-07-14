using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Parsing.Aff;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.Behaviours.EntityCreation
{

    public class TapEntityCreator : ECSMonoBehaviour
    {
        public static TapEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject tapNotePrefab;
        private Entity tapNoteEntityPrefab;
        [SerializeField] private GameObject arcTapNotePrefab;
        [SerializeField] private GameObject connectionLinePrefab;
        [SerializeField] private GameObject shadowPrefab;
        private Entity arcTapNoteEntityPrefab;
        private Entity connectionLineEntityPrefab;
        private Entity shadowEntityPrefab;
        private void Awake()
        {
            Instance = this;
            tapNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(tapNotePrefab, EntityManager);
            arcTapNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(arcTapNotePrefab, EntityManager);
            connectionLineEntityPrefab = GameObjectConversionSettings.ConvertToNote(connectionLinePrefab, EntityManager);
            shadowEntityPrefab = GameObjectConversionSettings.ConvertToNote(shadowPrefab, EntityManager);
        }

        public void CreateEntities(List<AffTap> affTapList, List<AffArcTap> affArcTapList)
        {
            affTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            affArcTapList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            int tapCount = affTapList.Count;
            int arcTapCount = affArcTapList.Count;
            int tapIndex = 0, arcTapIndex = 0;

            while (tapIndex < tapCount && arcTapIndex < arcTapCount) 
            {
                AffTap tap = affTapList[tapIndex];
                AffArcTap arctap = affArcTapList[tapIndex];

                if (tap.timing == arctap.timing)
                {
                    Entity tapEntity = CreateTapEntity(tap);
                    Entity arctapEntity = CreateArcTapEntity(arctap);
                    CreateConnection(tap, arctap, tapEntity, arctapEntity);
                    tapIndex++;
                    arcTapIndex++;
                }
                else if (tap.timing < arctap.timing)
                {
                    CreateTapEntity(tap);
                    tapIndex++;
                }
                else
                {
                    CreateArcTapEntity(arctap);
                    arcTapIndex++;
                }
            }

            while (tapIndex < tapCount)
            {
                CreateTapEntity(affTapList[tapIndex++]);
            }

            while (arcTapIndex < arcTapCount)
            {
                CreateArcTapEntity(affArcTapList[arcTapIndex++]);
            }
        }


        public Entity CreateTapEntity(AffTap tap)
        {
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
            
            return tapEntity;
        }

        public Entity CreateArcTapEntity(AffArcTap arctap)
        {
            Entity tapEntity = EntityManager.Instantiate(arcTapNoteEntityPrefab);
            Entity shadowEntity = EntityManager.Instantiate(shadowEntityPrefab);

            EntityManager.SetComponentData(tapEntity, new ArcTapShadowReference()
            {
                value = shadowEntity
            });

            float x = Conversion.GetWorldX(arctap.position.x);
            float y = Conversion.GetWorldY(arctap.position.y);
            const float z = 0;

            EntityManager.SetComponentData(tapEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            EntityManager.SetComponentData(shadowEntity, new Translation()
            {
                Value = new float3(x, 0, z)
            });

            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup);
            FloorPosition floorPositionF = new FloorPosition()
            {
                value = floorpos
            };

            EntityManager.SetComponentData(tapEntity, floorPositionF);
            EntityManager.SetComponentData(shadowEntity, floorPositionF);

            TimingGroup group = new TimingGroup()
            {
                value = arctap.timingGroup
            };

            EntityManager.SetComponentData(tapEntity, group);
            EntityManager.SetComponentData(shadowEntity, group);

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arctap.timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arctap.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            EntityManager.SetComponentData(tapEntity, new AppearTime(){ value = appearTime });

            //Judge entities
            EntityManager.SetComponentData(tapEntity, new ChartTime(arctap.timing));
            EntityManager.SetComponentData(tapEntity, new ChartPosition(Conversion.GetWorldPos(arctap.position)));

            ScoreManager.Instance.tracker.noteCount++;

            return tapEntity;
        }

        public void CreateConnection(AffTap tap, AffArcTap arctap, Entity tapEntity, Entity arcTapEntity)
        {
            Entity lineEntity = EntityManager.Instantiate(connectionLineEntityPrefab);

            float x = Conversion.GetWorldX(arctap.position.x);
            float y = Conversion.GetWorldY(arctap.position.y) - 0.5f;
            const float z = 0;

            float dx = Conversion.TrackToX(tap.track) - x;
            float dy = -y;

            float3 direction = new float3(dx, dy, 0);
            float length = math.sqrt(dx*dx + dy*dy);

            EntityManager.SetComponentData(lineEntity, new Translation(){
                Value = new float3(x, y, z)
            });

            EntityManager.AddComponentData(lineEntity, new NonUniformScale(){
                Value = new float3(1f, 1f, length)
            });
            
            EntityManager.SetComponentData(lineEntity, new Rotation(){
                Value = quaternion.LookRotationSafe(direction, new Vector3(0,0,1))
            });

            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup);
            EntityManager.AddComponentData(lineEntity, new FloorPosition(){
                value = floorpos
            });
            EntityManager.SetComponentData(lineEntity, new TimingGroup()
            {
                value = arctap.timingGroup
            });
            int appearTime = EntityManager.GetComponentData<AppearTime>(tapEntity).value;
            EntityManager.SetComponentData(lineEntity, new AppearTime(){
                value = appearTime
            });
            EntityManager.SetComponentData(tapEntity, new ConnectionReference(lineEntity));
            EntityManager.SetComponentData(arcTapEntity, new ConnectionReference(lineEntity));
        }
    }

}