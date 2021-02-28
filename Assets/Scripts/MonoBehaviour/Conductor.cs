using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField]
    private float offset;

    [HideInInspector]
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
    }
    public void SetOffset(int value)
    {
        offset = value; 
    }
    public void SetupTiming(List<List<AffTiming>> timingGroups) {
        //precalculate floorposition value for timing events
        //Unrolling the first loop. first one will also take on the job of creating beat divisor
        timingEventGroups = new List<List<TimingEvent>>(timingGroups.Count); 

        timingGroups[0].Sort( (item1, item2) => {return item1.timing.CompareTo(item2.timing);} );

        timingEventGroups.Add(new List<TimingEvent>(timingGroups[0].Count));

        timingEventGroups[0].Add(new TimingEvent(){
            timing = timingGroups[0][0].timing,
            floorPosition = 0,
            bpm = timingGroups[0][0].bpm
        });

        for (int i=1; i<timingGroups[0].Count; i++) 
        {
            timingEventGroups[0].Add(new TimingEvent(){
                timing = timingGroups[0][i].timing,
                floorPosition = timingGroups[0][i-1].bpm 
                              * (timingGroups[0][i].timing - timingGroups[0][i-1].timing)
                              + timingEventGroups[0][i-1].floorPosition,
                bpm = timingGroups[0][i].bpm
            });
        }
        //todo: beat divisor

        for (int i=1; i<timingGroups.Count; i++)
        {
            timingGroups[i].Sort( (item1, item2) => {return item1.timing.CompareTo(item2.timing);} );

            timingEventGroups.Add(new List<TimingEvent>(timingGroups[i].Count));

            timingEventGroups[i].Add(new TimingEvent(){
                timing = timingGroups[i][0].timing,
                floorPosition = 0,
                bpm = timingGroups[i][0].bpm
            });

            for (int j=1; j<timingGroups[i].Count; j++) 
            {
                timingEventGroups[i].Add(new TimingEvent(){
                    timing = timingGroups[i][j].timing,
                    floorPosition = timingGroups[i][j-1].bpm
                                  * (timingGroups[i][j].timing - timingGroups[i][j-1].timing)
                                  + timingEventGroups[i][j-1].floorPosition,
                    bpm = timingGroups[i][j].bpm
                });
            }
        }
        groupIndexCache = new List<int>(new int[timingEventGroups.Count]);
    }
    public float GetFloorPositionFromTiming(int timing, int timingGroup)
    {
        List<TimingEvent> group = timingEventGroups[timingGroup];
        //caching the index so we dont have to loop the entire thing every time
        //list access should be largely local anyway
        int i = groupIndexCache[timingGroup];

        while (i>0 && group[i].timing > timing)
        {
            i--;     
        }
        while (i<group.Count - 1 && group[i+1].timing < timing)
        {
            i++;
        }

        groupIndexCache[timingGroup] = i;

        return group[i].floorPosition + (timing - group[i].timing) * group[i].bpm;
    }
}