using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Arcaoid.Utility;

public class AffError
{
    public AffErrorType type;
    public int line;

    public AffError(AffErrorType type, int line)
    {
        this.type = type;
        this.line = line;
    }
}

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

public enum AffErrorType
{
    none,
    invalid_line,
    improper_int,
    improper_float,
    improper_boolean,
    improper_arctype,
    improper_camtype,
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
        AffError status = ReadChart(path);
        if (status != null)
            Debug.Log("Error at line " + status.line + " of type: " + status.type);
    }

    private AffError ReadChart(string path)
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
                        if (!lineParser.ParseInt(out int audioOff))
                            return new AffError(AffErrorType.improper_int, i);
                        SetAudioOffset(audioOff);
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
            AffErrorType errorType;

            switch (type)
            {
                case "timing":
                    errorType = AddTiming(lineParser, currentTimingGroup);
                    if (errorType != AffErrorType.none)
                        return new AffError(errorType, i+1);
                    break;
                case "":
                    errorType = AddTap(lineParser,currentTimingGroup);
                    if (errorType != AffErrorType.none)
                        return new AffError(errorType, i+1);
                    break;
                case "hold":
                    errorType = AddHold(lineParser,currentTimingGroup);
                    if (errorType != AffErrorType.none)
                        return new AffError(errorType, i+1);
                    break;
                case "arc":
                    errorType = AddArc(lineParser,currentTimingGroup);
                    if (errorType != AffErrorType.none)
                        return new AffError(errorType, i+1);
                    break;
                case "camera":
                    errorType = AddCamera(lineParser);
                    if (errorType != AffErrorType.none)
                        return new AffError(errorType, i+1);
                    break;
                case "scenecontrol":
                    errorType = AddSceneControlEvent(lineParser);
                    if (errorType != AffErrorType.none)
                        return new AffError(errorType, i+1);
                    break;
                case "timinggroup":
                    currentTimingGroup++;
                    affTimingList.Add(new List<AffTiming>());
                    break;
                default:
                    return new AffError(AffErrorType.invalid_line, i+1); 
            }
            
            i++;
        }

        Conductor.Instance.SetupTiming(affTimingList);
        TapEntityCreator.Instance.CreateEntities(affTapList);
        HoldEntityCreator.Instance.CreateEntities(affHoldList);
        ArcEntityCreator.Instance.CreateEntities(affArcList);

        return null;
    }

    private void SetAudioOffset(int offset)
    {
        Conductor.Instance.SetOffset(offset);
    }

    private AffErrorType AddTiming(StringParser lineParser, int currentTimingGroup)
    {
        if (!lineParser.ParseInt(out int timing, ","))
            return AffErrorType.improper_int;

        if (!lineParser.ParseFloat(out float bpm, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseFloat(out float divisor, ")"))
            return AffErrorType.improper_float;

        affTimingList[currentTimingGroup].Add(new AffTiming(){timing = timing, bpm = bpm, divisor = divisor});
        return AffErrorType.none;
    }

    private AffErrorType AddTap(StringParser lineParser, int currentTimingGroup)
    {
        if (!lineParser.ParseInt(out int timing, ","))
            return AffErrorType.improper_int;
        
        if (!lineParser.ParseInt(out int track, ")"))
            return AffErrorType.improper_int;

        affTapList.Add(new AffTap(){timing = timing, track = track, timingGroup = currentTimingGroup});
        return AffErrorType.none;
    }

    private AffErrorType AddHold(StringParser lineParser, int currentTimingGroup)
    {
        if (!lineParser.ParseInt(out int timing, ","))
            return AffErrorType.improper_int;

        if (!lineParser.ParseInt(out int endTiming, ","))
            return AffErrorType.improper_int;
        
        if (!lineParser.ParseInt(out int track, ")"))
            return AffErrorType.improper_int;

        affHoldList.Add(new AffHold(){timing = timing, endTiming = endTiming, track = track, timingGroup = currentTimingGroup});
        return AffErrorType.none;
    }

    private AffErrorType AddArc(StringParser lineParser, int currentTimingGroup)
    {

        if (!lineParser.ParseInt(out int timing, ","))
            return AffErrorType.improper_int;

        if (!lineParser.ParseInt(out int endTiming, ","))
            return AffErrorType.improper_int;

        if (!lineParser.ParseFloat(out float startX, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseFloat(out float endX, ","))
            return AffErrorType.improper_float;

        if (!GetEasingType(out ArcEasing easing, lineParser.ReadString(",")))
            return AffErrorType.improper_arctype;

        if (!lineParser.ParseFloat(out float startY, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseFloat(out float endY, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseInt(out int color, ","))
            return AffErrorType.improper_int;

        lineParser.SkipPast(",");

        if (!lineParser.ParseBool(out bool isTrace, ")"))
            return AffErrorType.improper_boolean;

        if (isTrace)
        {
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

        if (lineParser.Current != ";")
        {
            //parse arctap
            do
            {
                lineParser.Skip(8);
                if (!lineParser.ParseInt(out int t, ")"))
                    return AffErrorType.improper_int;

                float x = Convert.GetXAt(t, startX, endX, easing);
                float y = Convert.GetYAt(t, startY, endY, easing);

                affArcTapList.Add(new AffArcTap()
                {
                    timing = t,
                    position = new float2(x, y)
                });

            } while (lineParser.Current == ",");
        }

        return AffErrorType.none;

    }

    public bool GetEasingType(out ArcEasing easing, string easingString)
    {
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
                return false;
        }

        return true;
    }

    private AffErrorType AddCamera(StringParser lineParser)
    {
        if (!lineParser.ParseInt(out int timing, ","))
            return AffErrorType.improper_int;

        if (!lineParser.ParseFloat(out float xpos, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseFloat(out float ypos, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseFloat(out float zpos, ","))
            return AffErrorType.improper_float;

        //Arcade's coordinate system seems to be different to arcaea's

        if (!lineParser.ParseFloat(out float yrot, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseFloat(out float xrot, ","))
            return AffErrorType.improper_float;

        if (!lineParser.ParseFloat(out float zrot, ","))
            return AffErrorType.improper_float;

        if (!GetCameraEasing(out CameraEasing easing, lineParser.ReadString(",")))
            return AffErrorType.improper_camtype;

        if (!lineParser.ParseInt(out int duration, ","))
            return AffErrorType.improper_int;

        affCameraList.Add(new AffCamera()
        {
            timing = timing,
            position = new float3(-xpos, ypos, zpos),
            rotate = new float3(-xrot, -yrot, zrot),
            easing = easing,
            duration = duration
        });

        return AffErrorType.none;
    }

    private static bool GetCameraEasing(out CameraEasing easing, string easingString)
    {
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
                return false;
        }

        return true;
    }

    public AffErrorType AddSceneControlEvent(StringParser lineParser)
    {
        //todo: scenecontrol support, maybe 
        return AffErrorType.none;
    }
}