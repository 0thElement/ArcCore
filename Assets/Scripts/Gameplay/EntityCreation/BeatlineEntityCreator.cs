using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Gameplay.Parsing;
using ArcCore.Utilities;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.EntityCreation
{

    public class BeatlineEntityCreator
    {
        private Entity beatlineEntityPrefab;
        private EntityManager em;

        public BeatlineEntityCreator(World world, GameObject beatlinePrefab)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            beatlineEntityPrefab = gocs.ConvertToNote(beatlinePrefab, em);
        }

        public void CreateEntities(IChartParser parser)
        {
            var timings = parser.Timings[0];

            timings.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            //Extending the first event to before the song's starting point
            {
                TimingRaw firstTiming = timings[0];
                float start = -3000 - PlayManager.Conductor.FullOffset;

                float distanceBetweenTwoLine = firstTiming.bpm == 0 ? float.MaxValue :
                                                                    60000f / math.abs(firstTiming.bpm) * firstTiming.divisor;
                                
                if (distanceBetweenTwoLine != 0)
                {
                    for (float timing = 0; timing >= start; timing -= distanceBetweenTwoLine)
                    {
                        CreateLineAt(firstTiming, timing);
                    }
                } 
            }

            for (int i=0; i < timings.Count - 1; i++)
            {
                TimingRaw currentTiming = timings[i];
                int limit = timings[i+1].timing;

                float distanceBetweenTwoLine = currentTiming.bpm == 0 ? float.MaxValue : 
                                                                        60000f / math.abs(currentTiming.bpm) * currentTiming.divisor;

                if (distanceBetweenTwoLine == 0) continue;
                
                for (float timing = currentTiming.timing; timing < limit; timing += distanceBetweenTwoLine)
                {
                    CreateLineAt(currentTiming, timing);
                }
            }

            //Last timing event
            {
                TimingRaw lastTiming = timings[timings.Count-1];
                uint limit = PlayManager.Conductor.songLength;

                float distanceBetweenTwoLine = lastTiming.bpm == 0 ? float.MaxValue : 
                                                                    60000f / math.abs(lastTiming.bpm) * lastTiming.divisor;

                if (distanceBetweenTwoLine == 0) return;
                
                for (float timing = lastTiming.timing; timing < limit; timing += distanceBetweenTwoLine)
                {
                    CreateLineAt(lastTiming, timing);
                }
            }
        }

        private void CreateLineAt(TimingRaw timingEvent, float timingf)
        {
            Entity lineEntity = em.Instantiate(beatlineEntityPrefab);

            int timing = Mathf.RoundToInt(timingf);

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(timing, 0);

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, 0);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, 0);
            int appearTime = (t1 < t2) ? t1 : t2;
            int disappearTime = (t1 < t2) ? t2 : t1;

            em.SetComponentData(lineEntity, new FloorPosition(floorpos));
            em.SetComponentData(lineEntity, new AppearTime(appearTime));
            em.SetComponentData(lineEntity, new DestroyOnTiming(disappearTime));
        }
    }

}