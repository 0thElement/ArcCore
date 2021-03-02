using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.MonoBehaviours
{
    public struct TimingEvent
    {
        public int timing;
        public float floorPosition;
        public float bpm;
    }

    public class Conductor : MonoBehaviour
    {
        //Simple implementation with dspTime, might switch out to a deltaTime based solution if this is too unreliable
        //Conductor is responsible for playing the audio and making sure gameplay syncs. Most systems will refer to this class to get the current timing

        public static Conductor Instance { get; private set; }

        private AudioSource audioSource;

        public delegate void TimeCalculatedAction(float time);
        public event TimeCalculatedAction OnTimeCalculated;

        [SerializeField]
        private float offset;

        private float dspStartPlayingTime;
        [HideInInspector]
        public float receptorTime;
        [HideInInspector]
        public List<float> groupFloorPosition;
        private List<List<TimingEvent>> timingEventGroups;
        private List<int> groupIndexCache;

        public void Awake()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            PlayMusic();
        }

        public void PlayMusic()
        {
            dspStartPlayingTime = (float)AudioSettings.dspTime;
            audioSource.Play();
        }

        public void Update()
        {
            receptorTime = (float)(AudioSettings.dspTime - dspStartPlayingTime - offset);
            OnTimeCalculated?.Invoke(receptorTime);
        }
        public void SetOffset(int value)
        {
            offset = value;
        }
        public void SetupTiming(List<List<AffTiming>> timingGroups)
        {
            //precalculate floorposition value for timing events
            //Unrolling the first loop. first one will also take on the job of creating beat divisor
            timingEventGroups = new List<List<TimingEvent>>(timingGroups.Count);

            SetupTimingGroup(timingGroups, 0);
            //todo: beat divisor

            for (int i = 1; i < timingGroups.Count; i++)
            {
                SetupTimingGroup(timingGroups, i);
            }
            groupIndexCache = new List<int>(new int[timingEventGroups.Count]);

        }
        private void SetupTimingGroup(List<List<AffTiming>> timingGroups, int i)
        {
            timingGroups[i].Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            timingEventGroups.Add(new List<TimingEvent>(timingGroups[i].Count));

            timingEventGroups[i].Add(new TimingEvent()
            {
                timing = timingGroups[i][0].timing,
                floorPosition = 0,
                bpm = timingGroups[i][0].bpm
            });

            for (int j = 1; j < timingGroups[i].Count; j++)
            {
                timingEventGroups[i].Add(new TimingEvent()
                {
                    timing = timingGroups[i][j].timing,
                    floorPosition = timingGroups[i][j - 1].bpm
                                  * (timingGroups[i][j].timing - timingGroups[i][j - 1].timing)
                                  + timingEventGroups[i][j - 1].floorPosition,
                    bpm = timingGroups[i][j].bpm
                });
            }
        }
        public float GetFloorPositionFromTiming(int timing, int timingGroup)
        {
            List<TimingEvent> group = timingEventGroups[timingGroup];
            //caching the index so we dont have to loop the entire thing every time
            //list access should be largely local anyway
            int i = groupIndexCache[timingGroup];

            while (i > 0 && group[i].timing > timing)
            {
                i--;
            }
            while (i < group.Count - 1 && group[i + 1].timing < timing)
            {
                i++;
            }

            groupIndexCache[timingGroup] = i;

            return group[i].floorPosition + (timing - group[i].timing) * group[i].bpm;
        }

        public int GetTimingEventIndexFromTiming(int timing, int timingGroup)
        {
            int maxIdx = timingEventGroups[timingGroup].Count;

            for (int i = 1; i < maxIdx; i++)
            {
                if (timingEventGroups[timingGroup][i].timing > timing)
                {
                    return i - 1;
                }
            }

            return maxIdx - 1;
        }

        public TimingEvent GetTimingEventFromTiming(int timing, int timingGroup)
            => timingEventGroups[timingGroup][GetTimingEventIndexFromTiming(timing, timingGroup)];

        public TimingEvent GetTimingEvent(int timing, int timingGroup)
            => timingEventGroups[timingGroup][timing];

        public TimingEvent? GetNextTimingEventOrNull(int index, int timingGroup)
            => index + 1 >= timingEventGroups[timingGroup].Count ? null : timingEventGroups[timingGroup][index + 1];

        public int TimingEventListLength(int timingGroup)
            => timingEventGroups[timingGroup].Count;

    }
}