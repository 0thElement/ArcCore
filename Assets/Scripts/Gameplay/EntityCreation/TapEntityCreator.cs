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
using ArcCore.Parsing;

namespace ArcCore.Gameplay.EntityCreation
{
    public class TapEntityCreator
    {
        private Entity tapNoteEntityPrefab;
        private Entity arcTapNoteEntityPrefab;
        private Entity connectionLineEntityPrefab;
        private Entity shadowEntityPrefab;
        private EntityManager em;

        public TapEntityCreator(
            World world,
            GameObject tapNotePrefab,
            GameObject arcTapNotePrefab,
            GameObject connectionLinePrefab,
            GameObject shadowPrefab)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            tapNoteEntityPrefab = gocs.ConvertToNote(tapNotePrefab, em);
            arcTapNoteEntityPrefab = gocs.ConvertToNote(arcTapNotePrefab, em);
            connectionLineEntityPrefab = gocs.ConvertToNote(connectionLinePrefab, em);
            shadowEntityPrefab = gocs.ConvertToNote(shadowPrefab, em);
        }

        public void CreateEntities(IChartParser parser)
        {
            var taps = parser.Taps;
            var arctaps = parser.Arctaps;

            taps.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            arctaps.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            
            int tapCount = taps.Count;
            int arcTapCount = arctaps.Count;
            int tapIndex = 0, arcTapIndex = 0;

            while (tapIndex < tapCount && arcTapIndex < arcTapCount) 
            {
                TapRaw tap = taps[tapIndex];
                ArctapRaw arctap = arctaps[tapIndex];

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
                CreateTapEntity(taps[tapIndex++]);
            }

            while (arcTapIndex < arcTapCount)
            {
                CreateArcTapEntity(arctaps[arcTapIndex++]);
            }

        }
        public Entity CreateTapEntity(TapRaw tap)
        {
            Entity tapEntity = em.Instantiate(tapNoteEntityPrefab);

            float x = Conversion.TrackToX(tap.track);
            const float y = 0;
            const float z = 0;

            em.SetComponentData(tapEntity, new Translation(){ 
                Value = new float3(x, y, z)
            });

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(tap.timing, tap.timingGroup);
            em.SetComponentData(tapEntity, new FloorPosition(){
                value = floorpos 
            });
            em.SetComponentData(tapEntity, new TimingGroup()
            {
                value = tap.timingGroup
            });

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, tap.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, tap.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(tapEntity, new AppearTime(){ value = appearTime });
            
            em.SetComponentData(tapEntity, new ChartTime(tap.timing));
            em.SetComponentData(tapEntity, new ChartLane(tap.track));

            PlayManager.ScoreHandler.tracker.noteCount++;
            
            return tapEntity;
        }

        public Entity CreateArcTapEntity(ArctapRaw arctap)
        {
            Entity tapEntity = em.Instantiate(arcTapNoteEntityPrefab);
            Entity shadowEntity = em.Instantiate(shadowEntityPrefab);

            em.SetComponentData(tapEntity, new ArcTapShadowReference()
            {
                value = shadowEntity
            });

            float x = Conversion.GetWorldX(arctap.position.x);
            float y = Conversion.GetWorldY(arctap.position.y);
            const float z = 0;

            em.SetComponentData(tapEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            em.SetComponentData(shadowEntity, new Translation()
            {
                Value = new float3(x, 0, z)
            });

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup);
            FloorPosition floorPositionF = new FloorPosition()
            {
                value = floorpos
            };

            em.SetComponentData(tapEntity, floorPositionF);
            em.SetComponentData(shadowEntity, floorPositionF);

            TimingGroup group = new TimingGroup()
            {
                value = arctap.timingGroup
            };

            em.SetComponentData(tapEntity, group);
            em.SetComponentData(shadowEntity, group);

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arctap.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arctap.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(tapEntity, new AppearTime(){ value = appearTime });

            //Judge entities
            em.SetComponentData(tapEntity, new ChartTime(arctap.timing));
            em.SetComponentData(tapEntity, new ChartPosition(Conversion.GetWorldPos(arctap.position)));

            PlayManager.ScoreHandler.tracker.noteCount++;

            return tapEntity;
        }

        public void CreateConnection(TapRaw tap, ArctapRaw arctap, Entity tapEntity, Entity arcTapEntity)
        {
            Entity lineEntity = em.Instantiate(connectionLineEntityPrefab);

            float x = Conversion.GetWorldX(arctap.position.x);
            float y = Conversion.GetWorldY(arctap.position.y) - 0.5f;
            const float z = 0;

            float dx = Conversion.TrackToX(tap.track) - x;
            float dy = -y;

            float3 direction = new float3(dx, dy, 0);
            float length = math.sqrt(dx*dx + dy*dy);

            em.SetComponentData(lineEntity, new Translation(){
                Value = new float3(x, y, z)
            });

            em.AddComponentData(lineEntity, new NonUniformScale(){
                Value = new float3(1f, 1f, length)
            });
            
            em.SetComponentData(lineEntity, new Rotation(){
                Value = quaternion.LookRotationSafe(direction, new Vector3(0,0,1))
            });

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup);
            em.AddComponentData(lineEntity, new FloorPosition(){
                value = floorpos
            });
            em.SetComponentData(lineEntity, new TimingGroup()
            {
                value = arctap.timingGroup
            });
            int appearTime = em.GetComponentData<AppearTime>(tapEntity).value;
            em.SetComponentData(lineEntity, new AppearTime(){
                value = appearTime
            });
            em.SetComponentData(tapEntity, new ConnectionReference(lineEntity));
            em.SetComponentData(arcTapEntity, new ConnectionReference(lineEntity));
        }
    }
}