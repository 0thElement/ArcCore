using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Parsing.Data;
using ArcCore.Utilities.Extensions;
using ArcCore.Parsing;

namespace ArcCore.Gameplay.EntityCreation
{
    public class ArcTapEntityCreator
    {
        private Entity arcTapNoteEntityPrefab;
        private Entity connectionLineEntityPrefab;
        private Entity shadowEntityPrefab;

        private EntityManager em;

        public ArcTapEntityCreator(
            World world, GameObject arcTapNotePrefab, 
            GameObject connectionLinePrefab, 
            GameObject shadowPrefab)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            arcTapNoteEntityPrefab = gocs.ConvertToNote(arcTapNotePrefab, em);
            connectionLineEntityPrefab = gocs.ConvertToNote(connectionLinePrefab, em);
            shadowEntityPrefab = gocs.ConvertToNote(shadowPrefab, em);
        }

        public void CreateEntities(IChartParser parser)
        {
            var taps = parser.Taps;
            var arctaps = parser.Arctaps;

            arctaps.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            taps.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            int lowBound=0;

            foreach (ArctapRaw arctap in arctaps)
            {
                //Main entity
                Entity tapEntity = em.Instantiate(arcTapNoteEntityPrefab);
                Entity shadowEntity = em.Instantiate(shadowEntityPrefab);

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

                //Connection line
                while (lowBound < taps.Count && arctap.timing > taps[lowBound].timing)
                {
                    lowBound++;
                }
                //Iterated the whole list without finding anything
                if (lowBound >= taps.Count) continue;

                int highBound = lowBound;
                while (highBound < taps.Count && arctap.timing == taps[highBound].timing)
                {
                    highBound++;
                }
                //if lowbound's timing is greater than arctap's timing, that means there are no tap with the same timing
                //Range from lowbound to highbound are all taps with the same timing

                for (int j=lowBound; j<highBound; j++)
                {
                    if (arctap.timingGroup == taps[j].timingGroup)
                        CreateConnections(arctap, taps[j], appearTime);
                }

                em.SetComponentData(tapEntity, new EntityReference()
                {
                    value = shadowEntity
                });
            }

        }

        public void CreateConnections(ArctapRaw arctap, TapRaw tap, int appearTime)
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
            em.SetComponentData(lineEntity, new AppearTime(){
                value = appearTime
            });
            em.SetComponentData(lineEntity, new ChartTime(){
                value = arctap.timing
            });
        }
    }
}