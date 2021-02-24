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
    public void SetTimingSetting(List<List<TimingEvent>> timingEventGroups)
    {
        this.timingEventGroups = timingEventGroups; 

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