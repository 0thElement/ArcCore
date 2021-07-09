using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System;
using ArcCore.Gameplay.Utility;
using ArcCore.Parsing.Aff;
using ArcCore.Utilities;
using ArcCore.Gameplay.Systems;
using ArcCore.Gameplay.Systems.Judgement;

namespace ArcCore.Gameplay.Behaviours
{
    public class Conductor : MonoBehaviour
    {
        //Simple implementation with dspTime, might switch out to a deltaTime based solution if this is too unreliable
        //Conductor is responsible for playing the audio and making sure gameplay syncs. Most systems will refer to this class to get the current timing

        /// <summary>
        /// The offset which is applied when first playing a song.
        /// </summary>
        public const double StartPlayOffset = 2.0;
        /// <summary>
        /// The amount which speed values are scaled at chart initialization.
        /// </summary>
        private const float SpeedCalculationFactor = 1 / 25f;

        public static Conductor Instance { get; private set; }

        /// <summary>
        /// The source which audio will be played from.
        /// This must be a component of this game object.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// The offset of the current charts, in milliseconds.
        /// </summary>
        public int offset;
        /// <summary>
        /// The current speed which the chart will be played at.
        /// This is treated as a direct multiplier of the original speed.
        /// </summary>
        [Range(1f,6.5f)] public float chartSpeed;

        /// <summary>
        /// The value of <see cref="AudioSettings.dspTime"/> at which the current song started playing.
        /// </summary>
        [HideInInspector] private double dspStartPlayingTime;

        /// <summary>
        /// The jagged array of timing events, indexed first by group, and then by order.
        /// It should always hold true that the first timing event of every group has a floor position
        /// of 0, and a timing value of 0.
        /// </summary>
        private TimingEvent[][] timingEventGroups;
        /// <summary>
        /// An array which stores the indices of the last accessed timing event for each group.
        /// This is used to speed up calculations which require the current-most timing event.
        /// </summary>
        private int[] groupIndexCache;

        /// <summary>
        /// The value, in milliseconds, of time passed since the start of this chart.
        /// </summary>
        [HideInInspector]
        public int receptorTime;
        /// <summary>
        /// The time of the last audio mix, accessed through <see cref="TimeSimple.Ticks"/>
        /// </summary>
        [HideInInspector]
        public long timeOfLastMix;

        /// <summary>
        /// The length of the current song in milliseconds.
        /// <para>
        /// Note that the 32-bit integer limit forces all songs to be less than approximately 0.27 years long.
        /// </para>
        /// </summary>
        [HideInInspector]
        public uint songLength;

        /// <summary>
        /// The current floor positions of all timing-groups.
        /// </summary>
        [HideInInspector]
        public NativeArray<float> currentFloorPosition;
        /// <summary>
        /// The actual speed used in calculating the positions of notes.
        /// </summary>
        private float scrollSpeed;

        private bool _isUpdating;
        /// <summary>
        /// Whether or not time values are currently being updated on this instance.
        /// </summary>
        [HideInInspector]
        public bool IsUpdating
        {
            get => _isUpdating;
            set
            {
                if(value != _isUpdating)
                {
                    _isUpdating = value;
                    SetSystemUpdates();
                }
            }
        }

        // REQUIRED THINGS //
        [SerializeField]
        private GameplayCamera gameplayCamera;
        [SerializeField]
        private InputVisualFeedback inputVisualFeedback;
        [SerializeField]
        private InputManager inputManager;
        
        public void SetSystemUpdates()
        {
            ExpirableJudgeSystem.Instance.Enabled = IsUpdating;
            FinalJudgeSystem.Instance.Enabled = IsUpdating;
            HoldHighlightSystem.Instance.Enabled = IsUpdating;
            ParticleJudgeSystem.Instance.Enabled = IsUpdating;
            TappableJudgeSystem.Instance.Enabled = IsUpdating;
            UnlockedHoldJudgeSystem.Instance.Enabled = IsUpdating;

            ChunkScopingSystem.Instance.Enabled = IsUpdating;
            JudgeEntitiesScopingSystem.Instance.Enabled = IsUpdating;
            MovingNotesSystem.Instance.Enabled = IsUpdating;
            ScaleAlongTrackSystem.Instance.Enabled = IsUpdating;

            gameplayCamera.isUpdating = IsUpdating;
            inputVisualFeedback.isUpdating = IsUpdating;
        }

        public void Awake()
        {
            //Set the static instance to this object
            Instance = this;
            _isUpdating = false;
            SetSystemUpdates();
        }
        
        /// <summary>
        /// Perform all needed actions to play a song after a delay of <see cref="StartPlayOffset"/> seconds.
        /// </summary>
        public void PlayMusic()
        {
            //Find the audio source
            audioSource = GetComponent<AudioSource>();

            //Setup song speed: http://answers.unity.com/answers/1677904/view.html
            audioSource.pitch = GameSettings.SongSpeed;
            Debug.Log(audioSource.outputAudioMixerGroup.audioMixer);
            audioSource.outputAudioMixerGroup.audioMixer.SetFloat("Pitch", 1 / GameSettings.SongSpeed);
            
            //Get song length
            songLength = (uint)Mathf.Round(audioSource.clip.length / GameSettings.SongSpeed * 1000);

            //Set timing information
            dspStartPlayingTime = AudioSettings.dspTime + StartPlayOffset;
            timeOfLastMix = TimeSimple.Ticks;

            //Schedule playing of song
            audioSource.PlayScheduled(dspStartPlayingTime);

            //Set updating
            IsUpdating = true;
        }

        public void Update()
        {
            if (!IsUpdating) return;

            //Calculate the current time
            receptorTime = 
                Mathf.RoundToInt( 
                    (float)(
                        AudioSettings.dspTime - dspStartPlayingTime + 
                        TimeSimple.TimeSinceTicksToSec(timeOfLastMix)
                    ) * 1000
                ) - offset;

            //Update floor positions
            UpdateCurrentFloorPosition();

            //Poll for input
            inputManager.PollInput();
        }

        public void OnAudioFilterRead(float[] _data, int _channels)
        {
            //Get time of last mix using audio filter hook
            timeOfLastMix = TimeSimple.Ticks;
        }

        public void OnDestroy()
        {
            //Responsably dispose things :D
            currentFloorPosition.Dispose();
        }

        /// <summary>
        /// (Re)Calculate the scroll speed to be used.
        /// </summary>
        public void CalculateScrollSpeed()
        {
            scrollSpeed = -chartSpeed / timingEventGroups[0][0].bpm * SpeedCalculationFactor;
        }

        /// <summary>
        /// Create timing event groups to track timing events given raw aff timing components.
        /// </summary>
        /// <param name="timingGroups">The raw aff timing components.</param>
        public void SetupTimingGroups(List<List<AffTiming>> timingGroups) 
        {
            timingEventGroups = new TimingEvent[timingGroups.Count][]; 
            currentFloorPosition = new NativeArray<float>(new float[timingGroups.Count], Allocator.Persistent);

            Debug.Log(timingGroups.Count);

            for (int i=0; i<timingGroups.Count; i++)
            {
                SetupTimingGroup(timingGroups, i);
            }

            groupIndexCache = new int[timingEventGroups.Length];
            CalculateScrollSpeed();
        }

        /// <summary>
        /// Create necessary information for a given timing event group, including correct base floor positions.
        /// </summary>
        /// <param name="timingGroups">The raw aff timing components.</param>
        /// <param name="i">The index of the timing group to set up.</param>
        private void SetupTimingGroup(List<List<AffTiming>> timingGroups, int i)
        {
            var tg = timingGroups[i];

            tg.Sort((item1, item2) => item1.timing.CompareTo(item2.timing));

            timingEventGroups[i] = new TimingEvent[tg.Count];

            timingEventGroups[i][0] = new TimingEvent
            {
                timing = tg[0].timing,
                baseFloorPosition = 0,
                bpm = tg[0].bpm
            };

            int count = tg.Count;
            for (int j = 1; j < count; j++)
            {
                timingEventGroups[i][j] = new TimingEvent
                {
                    timing = tg[j].timing,
                    //Calculate the base floor position
                    baseFloorPosition = tg[j - 1].bpm
                                * (tg[j].timing - tg[j - 1].timing)
                                + timingEventGroups[i][j - 1].baseFloorPosition,
                    bpm = tg[j].bpm
                };
            }
        }

        /// <summary>
        /// Get the floor position for a given time in the chart.
        /// </summary>
        /// <param name="timing">The time, in milliseconds, to get the floorpos of.</param>
        /// <param name="timingGroup">The timing group to look into when calculating the floorpos.</param>
        /// <returns>The real floorpos, with <see cref="scrollSpeed"/> accounted for.</returns>
        public float GetFloorPositionFromTiming(int timing, int timingGroup)
        {
            TimingEvent[] group = timingEventGroups[timingGroup];

            if (timing<group[0].timing) return group[0].bpm*(timing - group[0].timing) * scrollSpeed;

            //caching the index so we dont have to loop the entire thing every time
            //list access should be largely local anyway
            int i = groupIndexCache[timingGroup];

            while (i > 0 && group[i].timing > timing)
            {
                i--;
            }
            while (i < group.Length - 1 && group[i + 1].timing < timing)
            {
                i++;
            }

            groupIndexCache[timingGroup] = i;

            return (group[i].baseFloorPosition + (timing - group[i].timing) * group[i].bpm) * scrollSpeed;
        }

        /// <summary>
        /// Get the index into <see cref="timingEventGroups"/> of the timing event in effect for a given time.
        /// </summary>
        /// <param name="timing">The time to check against, in milliseconds.</param>
        /// <param name="timingGroup">The timing group to look into.</param>
        /// <returns>The index of the timing event which is in effect at <paramref name="timing"/> in group <paramref name="timingGroup"/></returns>
        public int GetTimingEventIndexFromTiming(int timing, int timingGroup)
        {
            int maxIdx = timingEventGroups[timingGroup].Length;

            for (int i = 1; i < maxIdx; i++)
            {
                if (timingEventGroups[timingGroup][i].timing > timing)
                {
                    return i - 1;
                }
            }

            return maxIdx - 1;
        }

        /// <summary>
        /// Get the timing event in effect for a given time.
        /// </summary>
        /// <param name="timing">The time to check against, in milliseconds.</param>
        /// <param name="timingGroup">The timing group to look into.</param>
        /// <returns>The timing event which is in effect at <paramref name="timing"/> in group <paramref name="timingGroup"/></returns>
        public TimingEvent GetTimingEventFromTiming(int timing, int timingGroup)
            => timingEventGroups[timingGroup][GetTimingEventIndexFromTiming(timing, timingGroup)];

        /// <summary>
        /// Get the next timing event in the given timing group if it exists, otherwise get <see langword="null"/>.
        /// </summary>
        /// <param name="index">The index to start from.</param>
        /// <param name="timingGroup">The timing group.</param>
        /// <returns>The next timing event after <paramref name="index"/> in the given timing group, or <see langword="null"/> if it does not exist.</returns>
        public TimingEvent? GetNextTimingEventOrNull(int index, int timingGroup)
            => index + 1 >= timingEventGroups[timingGroup].Length ? 
                (TimingEvent?)null : 
                timingEventGroups[timingGroup][index + 1];

        /// <summary>
        /// Get the first time at which the floor position of a given timing group is equal to a given value.
        /// </summary>
        /// <param name="floorposition">The target value of the timing group's floor position.</param>
        /// <param name="timingGroup">The timing group to search in.</param>
        /// <returns>The first time at which the floor postion of group <paramref name="timingGroup"/> is equal to <paramref name="floorposition"/></returns>
        public int GetFirstTimingFromFloorPosition(float floorposition, int timingGroup)
        {
            int maxIndex = timingEventGroups[timingGroup].Length;
            floorposition /= scrollSpeed;

            TimingEvent first = timingEventGroups[timingGroup][0];
            if (first.bpm * (floorposition - first.baseFloorPosition) < 0) return Mathf.RoundToInt((floorposition - first.baseFloorPosition)/ first.bpm);

            for (int i = 0; i < maxIndex - 1; i++)
            {
                TimingEvent curr = timingEventGroups[timingGroup][i];
                TimingEvent next = timingEventGroups[timingGroup][i+1];

                if ((curr.baseFloorPosition < floorposition && next.baseFloorPosition > floorposition)
                ||  (curr.baseFloorPosition > floorposition && next.baseFloorPosition < floorposition))
                {
                    float result = (floorposition - curr.baseFloorPosition) / curr.bpm + curr.timing;
                    return Mathf.RoundToInt(result);
                }
            }

            TimingEvent last = timingEventGroups[timingGroup][maxIndex-1];
            float lastresult =  (floorposition - last.baseFloorPosition) / last.bpm + last.timing;
            return Mathf.RoundToInt(lastresult);
        }

        /// <summary>
        /// Update the current floor postion values.
        /// </summary>
        public void UpdateCurrentFloorPosition()
        {
            //Might separate the output array into its own singleton class or entity
            for (int group = 0; group < timingEventGroups.Length; group++)
            {
                currentFloorPosition[group] = GetFloorPositionFromTiming(receptorTime, group);
            }
        }
    }
}