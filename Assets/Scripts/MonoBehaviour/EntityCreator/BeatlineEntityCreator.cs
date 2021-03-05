using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Data;
using ArcCore.Structs;

namespace ArcCore.MonoBehaviours.EntityCreation
{

    public class BeatlineEntityCreator : MonoBehaviour
    {
        public static BeatlineEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject BeatlinePrefab;
        private Entity BeatlineEntityPrefab;
        private World defaultWorld;
        private EntityManager entityManager;
        private void Awake()
        {
            Instance = this;
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
            BeatlineEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(BeatlinePrefab, settings);
        }

        public void CreateEntities(List<AffTiming> affTimingList)
        {
            affTimingList.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            //Extending the first event to before the song's starting point
            {
                AffTiming firstTiming = affTimingList[0];
                float start = -3000 - Conductor.Instance.offset;

                float distanceBetweenTwoLine = firstTiming.bpm == 0 ? float.MaxValue :
                                                                    60000f / math.abs(firstTiming.bpm) * firstTiming.divisor;
                                
                if (distanceBetweenTwoLine != 0)
                {
                    for (float timing = start; timing < 0; timing+=distanceBetweenTwoLine)
                    {
                        CreateLineAt(firstTiming, timing);
                    }
                } 
            }

            for (int i=0; i < affTimingList.Count - 1; i++)
            {
                AffTiming currentTiming = affTimingList[i];
                int limit = affTimingList[i+1].timing;

                float distanceBetweenTwoLine = currentTiming.bpm == 0 ? float.MaxValue : 
                                                                        60000f / math.abs(currentTiming.bpm) * currentTiming.divisor;

                if (distanceBetweenTwoLine == 0 || distanceBetweenTwoLine > limit - currentTiming.timing) continue;
                
                for (float timing = currentTiming.timing; timing < limit; timing+=distanceBetweenTwoLine)
                {
                    CreateLineAt(currentTiming, timing);
                }
            }

            //Last timing event
            {
                AffTiming lastTiming = affTimingList[affTimingList.Count-1];
                int limit = Conductor.Instance.songLength;

                float distanceBetweenTwoLine = lastTiming.bpm == 0 ? float.MaxValue : 
                                                                    60000f / math.abs(lastTiming.bpm) * lastTiming.divisor;

                if (distanceBetweenTwoLine == 0 || distanceBetweenTwoLine > limit - lastTiming.timing) return;
                
                for (float timing = lastTiming.timing; timing < limit; timing += distanceBetweenTwoLine)
                {
                    CreateLineAt(lastTiming, timing);
                }
            }
        }

        private void CreateLineAt(AffTiming timingEvent, float timingf)
        {
            int timing = (int)Mathf.Round(timingf);

            Entity lineEntity = entityManager.Instantiate(BeatlineEntityPrefab);

            fixed_dec floorposition = Conductor.Instance.GetFloorPositionFromTiming(timing, 0);

            entityManager.SetComponentData<FloorPosition>(lineEntity, new FloorPosition(){
                Value = floorposition
            });
        }
    }

}