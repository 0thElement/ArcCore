using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System;
using ArcCore.Gameplay.Utility;
using ArcCore.Parsing.Aff;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Behaviours
{
    public class Conductor : MonoBehaviour
    {
        //Simple implementation with dspTime, might switch out to a deltaTime based solution if this is too unreliable
        //Conductor is responsible for playing the audio and making sure gameplay syncs. Most systems will refer to this class to get the current timing

        public static Conductor Instance { get; private set; }
        private AudioSource audioSource;

        [SerializeField] public int offset;
        [SerializeField, Range(1f,6.5f)] public float chartSpeed;
        [HideInInspector] private float dspStartPlayingTime;
        [HideInInspector] public List<float> groupFloorPosition;
        private List<List<TimingEvent>> timingEventGroups;
        private List<int> groupIndexCache;
        public int receptorTime;
        public long timeOfLastMix;
        public int songLength;
        public NativeArray<float> currentFloorPosition;
        private float scrollSpeed;
        
        public void Awake()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            timeOfLastMix = TimeSimple.Ticks;
            songLength = (int)Mathf.Round(audioSource.clip.length*1000);
        }
        
        public void PlayMusic()
        {
            dspStartPlayingTime = (float)AudioSettings.dspTime + 1f;
            audioSource.PlayScheduled(dspStartPlayingTime);
        }

        public void Update()
        {
            receptorTime = Mathf.RoundToInt(
                (float)(AudioSettings.dspTime - dspStartPlayingTime + TimeSimple.TimeSinceTicksToSec(timeOfLastMix)) * 1000)
                - offset;
            UpdateCurrentFloorPosition();

            InputManager.Instance.PollInput();
        }
        public void SetOffset(int value)
        {
            offset = value; 
        }
        public void OnAudioFilterRead(float[] data, int channels)
        {
            timeOfLastMix = TimeSimple.Ticks;
        }
        public void OnDestroy()
        {
            currentFloorPosition.Dispose();
        }
        public void SetupTiming(List<List<AffTiming>> timingGroups) {
            //precalculate floorposition value for timing events

            scrollSpeed = -chartSpeed / timingGroups[0][0].bpm / 25f;
            timingEventGroups = new List<List<TimingEvent>>(timingGroups.Count); 

            currentFloorPosition = new NativeArray<float>(new float[timingGroups.Count], Allocator.Persistent);

            for (int i=0; i<timingGroups.Count; i++)
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

            if (timing<group[0].timing) return group[0].bpm*(timing - group[0].timing) * scrollSpeed;

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

            return (group[i].floorPosition + (timing - group[i].timing) * group[i].bpm) * scrollSpeed;
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
            => index + 1 >= timingEventGroups[timingGroup].Count ? (TimingEvent?)null : timingEventGroups[timingGroup][index + 1];

        public int TimingEventListLength(int timingGroup)
            => timingEventGroups[timingGroup].Count;
        public int GetFirstTimingFromFloorPosition(float floorposition, int timingGroup)
        {
            int maxIndex = timingEventGroups[timingGroup].Count;
            floorposition /= scrollSpeed;

            TimingEvent first = timingEventGroups[timingGroup][0];
            if (first.bpm * (floorposition - first.floorPosition) < 0) return Mathf.RoundToInt((floorposition - first.floorPosition)/ first.bpm);

            for (int i = 0; i < maxIndex - 1; i++)
            {
                TimingEvent curr = timingEventGroups[timingGroup][i];
                TimingEvent next = timingEventGroups[timingGroup][i+1];

                if ((curr.floorPosition < floorposition && next.floorPosition > floorposition)
                ||  (curr.floorPosition > floorposition && next.floorPosition < floorposition))
                {
                    float result = (floorposition - curr.floorPosition) / curr.bpm + curr.timing;
                    return Mathf.RoundToInt(result);
                }
            }

            TimingEvent last = timingEventGroups[timingGroup][maxIndex-1];
            float lastresult =  (floorposition - last.floorPosition) / last.bpm + last.timing;
            return Mathf.RoundToInt(lastresult);
        }

        public void UpdateCurrentFloorPosition()
        {
            if (timingEventGroups == null) return;
            //Might separate the output array into its own singleton class or entity
            for (int group=0; group < timingEventGroups.Count; group++)
            {
                currentFloorPosition[group] = GetFloorPositionFromTiming(receptorTime, group);
            }
        }
    }
}