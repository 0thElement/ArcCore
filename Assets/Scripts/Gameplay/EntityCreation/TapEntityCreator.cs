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
        private EntityManager em;

        public TapEntityCreator(World world, GameObject tapNotePrefab)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            tapNoteEntityPrefab = gocs.ConvertToNote(tapNotePrefab, em);
        }

        public void CreateEntities(IChartParser parser)
        {
            var taps = parser.Taps;

            taps.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (TapRaw tap in taps)
            {
                //Main Entity
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
            }
        }
    }
}