using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Arcaoid.Utility;

public struct AffTiming
{
    public int timing;
    public float bpm;
    public float divisor;
}

public struct AffTap
{
    public int timing;
    public int track;
    public int timingGroup;
}

public struct AffHold
{
    public int timing;
    public int endTiming;
    public int track;
    public int timingGroup;
}

public struct AffArc
{
    public int timing;
    public int endTiming;
    public float startX;
    public float endX;
    public ArcEasing easing;
    public float startY;
    public float endY;
    public int timingGroup;
}

public struct AffTrace
{
    public int timing;
    public int endTiming;
    public float startX;
    public float endX;
    public ArcEasing easing;
    public float startY;
    public float endY;
    public int color;
    public int timingGroup;
}

public struct AffArcTap
{
    public int timing;
    public float2 position;
}

public struct AffCamera
{
    public int timing;
    public float3 position;
    public float3 rotate;
    public CameraEasing easing;
    public int duration;
}

public struct AffSceneControlEvent
{
    public int timing;
}

public enum ArcEasing
{
    b,
    s,
    si,
    so,
    sisi,
    soso,
    siso,
    sosi
}

public enum CameraEasing
{
    l,
    qi,
    qo,
    s,
    reset
}

public class ChartReader : MonoBehaviour
{
    public static ChartReader Instance { get; private set; }

    private string path;
    
    private List<List<AffTiming>> affTimingList = new List<List<AffTiming>>();
    private List<AffTap> affTapList = new List<AffTap>();
    private List<AffHold> affHoldList = new List<AffHold>();
    private List<List<AffArc>> affArcList = new List<List<AffArc>>();
    private List<AffTrace> affTraceList = new List<AffTrace>();
    private List<AffArcTap> affArcTapList = new List<AffArcTap>();
    private List<AffCamera> affCameraList = new List<AffCamera>();
    private List<AffSceneControlEvent> affSceneControlEventList = new List<AffSceneControlEvent>();

    private void Awake()
    {
        Instance = this;
        // Temporary
        path = Path.Combine(Application.dataPath, "TempAssets", "2.aff");
        ReadChart(path);
    }

    private void ReadChart(string path)
    {
        string[] lines = File.ReadAllLines(path);

        //Read all header options
        int i=0;
        while (i<lines.Length && lines[i][0]!='-') {
            if (lines[i].IndexOf(":") != -1) {
                StringParser lineParser = new StringParser(lines[i]);

                string option = lineParser.ReadString(":");

                switch (option)
                {
                    case "AudioOffset":
                        SetAudioOffset(lineParser.ReadInt());
                        break;
                }
            }
            i++;
        }

        //Read notes
        i++;
        int currentTimingGroup = 0;
        affTimingList.Add(new List<AffTiming>());
        while (i<lines.Length) {

            StringParser lineParser = new StringParser(lines[i]);
            string type = lineParser.ReadString("(");

            switch (type)
            {
                case "timing":
                    AddTiming(lineParser,currentTimingGroup);
                    break;
                case "":
                    AddTap(lineParser,currentTimingGroup);
                    break;
                case "hold":
                    AddHold(lineParser,currentTimingGroup);
                    break;
                case "arc":
                    AddArc(lineParser,currentTimingGroup);
                    break;
                case "camera":
                    AddCamera(lineParser);
                    break;
                case "scenecontrol":
                    AddSceneControlEvent(lineParser);
                    break;
                case "timinggroup":
                    currentTimingGroup++;
                    affTimingList.Add(new List<AffTiming>());
                    break;
            }
            
            i++;
        }

        Conductor.Instance.SetupTiming(affTimingList);
        TapEntityCreator.Instance.CreateEntities(affTapList);
        HoldEntityCreator.Instance.CreateEntities(affHoldList);
        ArcEntityCreator.Instance.CreateEntities(affArcList);
    }

    private void SetAudioOffset(int offset)
    {
        Conductor.Instance.SetOffset(offset);
    }

    private void AddTiming(StringParser lineParser, int currentTimingGroup)
    {
        int timing = lineParser.ReadInt(",");
        float bpm = lineParser.ReadFloat(",");
        float divisor = lineParser.ReadFloat(")");

        affTimingList[currentTimingGroup].Add(new AffTiming(){timing = timing, bpm = bpm, divisor = divisor});
    }

    private void AddTap(StringParser lineParser, int currentTimingGroup)
    {
        int timing = lineParser.ReadInt(",");
        int track = lineParser.ReadInt(")");

        affTapList.Add(new AffTap(){timing = timing, track = track, timingGroup = currentTimingGroup});
    }

    private void AddHold(StringParser lineParser, int currentTimingGroup)
    {
        int timing = lineParser.ReadInt(",");
        int endTiming = lineParser.ReadInt(",");
        int track = lineParser.ReadInt(")");

        affHoldList.Add(new AffHold(){timing = timing, endTiming = endTiming, track = track, timingGroup = currentTimingGroup});
    }

    private void AddArc(StringParser lineParser, int currentTimingGroup)
    {
        int timing = lineParser.ReadInt(",");
        int endTiming = lineParser.ReadInt(",");
        float startX = lineParser.ReadFloat(",");
        float endX = lineParser.ReadFloat(",");
        string easingString = lineParser.ReadString(",");
        ArcEasing easing;
        switch (easingString)
        {
            case "b":
                easing = ArcEasing.b;
                break;
            case "s":
                easing = ArcEasing.s;
                break;
            case "si":
                easing = ArcEasing.si;
                break;
            case "so":
                easing = ArcEasing.so;
                break;
            case "sisi":
                easing = ArcEasing.sisi;
                break;
            case "soso":
                easing = ArcEasing.soso;
                break;
            case "siso":
                easing = ArcEasing.siso;
                break;
            case "sosi":
                easing = ArcEasing.sosi;
                break;
            default:
                easing = ArcEasing.b;
                break;
        }
        float startY = lineParser.ReadFloat(",");
        float endY = lineParser.ReadFloat(",");
        int color = lineParser.ReadInt(",");
        lineParser.ReadString(",");
        bool isTrace = lineParser.ReadBool(")");

        if (isTrace) {
            while (affArcList.Count < color + 1) affArcList.Add(new List<AffArc>());
            affArcList[color].Add(new AffArc()
            {
                timing = timing, 
                endTiming = endTiming,
                startX = startX,
                endX = endX,
                easing = easing,
                startY = startY,
                endY = endY,
                timingGroup = currentTimingGroup
            });
        }
        else affTraceList.Add(new AffTrace()
        {
            timing = timing,
            endTiming = endTiming,
            startX = startX,
            endX = endX,
            easing = easing,
            startY = startY,
            endY = endY,
            color = color,
            timingGroup = currentTimingGroup
        });
        
        if (lineParser.Current != ";") {
            //parse arctap
            do
            {
                lineParser.Skip(8);
                int t = lineParser.ReadInt(")");

                float x = Convert.GetXAt(t, startX, endX, easing);
                float y = Convert.GetYAt(t, startY, endY, easing);
                
                affArcTapList.Add(new AffArcTap()
                {
                    timing = t,
                    position = new float2(x, y)
                });

            } while (lineParser.Current==",");
        }
    }

    private void AddCamera(StringParser lineParser)
    {
        int timing = lineParser.ReadInt(",");
        //Arcade's coordinate system seems to be different to arcaea's
        float3 position = new float3(-lineParser.ReadFloat(","), lineParser.ReadFloat(","), lineParser.ReadFloat(","));
        float rotateY = -lineParser.ReadFloat(",");    
        float rotateX = -lineParser.ReadFloat(",");    
        float3 rotate = new float3(rotateX, rotateY, lineParser.ReadFloat(","));

        string easingString = lineParser.ReadString(",");
        CameraEasing easing;
        switch (easingString)
        {
            case "l":
                easing = CameraEasing.l;
                break;
            case "qi":
                easing = CameraEasing.qi;
                break;
            case "qo":
                easing = CameraEasing.qo;
                break;
            case "s":
                easing = CameraEasing.s;
                break;
            case "reset":
                easing = CameraEasing.reset;
                break;
            default:
                easing = CameraEasing.reset;
                break;
        }

        int duration = lineParser.ReadInt(")");

        affCameraList.Add(new AffCamera()
        {
            timing = timing,
            position = position,
            rotate = rotate,
            easing = easing,
            duration = duration
        });
    }

    public void AddSceneControlEvent(StringParser lineParser)
    {
    //todo: scenecontrol support, maybe 
    }
}