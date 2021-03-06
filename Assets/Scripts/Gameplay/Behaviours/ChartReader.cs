using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Behaviours.EntityCreation;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Parsing.Aff;
using System.Text.RegularExpressions;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Behaviours
{
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
            Application.targetFrameRate = 200;
        }
        private void Start()
        {
            // Temporary
            AffError err;
            if ((err = ReadChart(Constants.GetDebugChart())) != null)
                Debug.LogError(err);
        }

        private void ReadChartNew(string path)
        {
            string text = File.ReadAllText(path);
            StringParserNew parser = new StringParserNew(text);

            //Header
            parser.SkipWhitespace();
            while(parser.Current != '-') 
            { 
                switch(parser.GetSection(end: ":", exceptionMessage: "Invalid header syntax!"))
                {
                    case "AudioOffset":

                        parser.SkipWhitespace();
                        int off = parser.GetInt(ends: StringParserNew.Whitespace, exceptionMessage: "Not a valid audio offset!");

                        SetAudioOffset(off);

                        parser.SkipPastAll(StringParserNew.MidSpace);
                        parser.Require(StringParserNew.LineEnd);

                        break;
                }
            }

            parser.SkipPast("-");
            while(!parser.EOSReached)
            {
                //BLAHBLAHBALH
            }
        }

        private AffError ReadChartFromFile(string path)
        {
            return ReadChart(File.ReadAllText(path));
        }
        private AffError ReadChart(string data)
        {
            string[] lines = Regex.Split(data, "\n\r|\r\n|\r|\n");

            for(int l = 0; l < lines.Length; l++)
            {
                lines[l] = lines[l].Trim();
            }

            //Read all header options
            int i = 0;

            //Local funcs cuz f*ck this
            AffError getError(AffErrorType t)
                => new AffError(t, i + 1);

            while (i < lines.Length && lines[i][0] != '-')
            {
                if (lines[i].IndexOf(":") != -1)
                {
                    StringParser lineParser = new StringParser(lines[i]);

                    if (!lineParser.ReadString(out string option, ":"))
                        return getError(AffErrorType.no_found_item);

                    switch (option)
                    {
                        case "AudioOffset":
                            if (!lineParser.ParseInt(out int audioOff))
                                return getError(AffErrorType.invalid_audio_offset);
                            SetAudioOffset(audioOff);
                            break;
                        default:
                            return getError(AffErrorType.invalid_header_option);
                    }
                }
                i++;
            }

            //Read notes
            i++;
            int currentTimingGroup = 0;
            affTimingList.Add(new List<AffTiming>());

            while (i < lines.Length)
            {
                if (lines[i][0] == '}' || lines[i][0] == '{')
                {
                    i++;
                    continue;
                }

                StringParser lineParser = new StringParser(lines[i]);

                if (!lineParser.ReadString(out string type, "("))
                    return getError(AffErrorType.invalid_line_format);

                AffErrorType errorType;

                switch (type)
                {
                    case "timing":
                        errorType = AddTiming(lineParser, currentTimingGroup);
                        if (errorType != AffErrorType.none)
                            return getError(errorType);
                        break;
                    case "":
                        errorType = AddTap(lineParser, currentTimingGroup);
                        if (errorType != AffErrorType.none)
                            return getError(errorType);
                        break;
                    case "hold":
                        errorType = AddHold(lineParser, currentTimingGroup);
                        if (errorType != AffErrorType.none)
                            return getError(errorType);
                        break;
                    case "arc":
                        errorType = AddArc(lineParser, currentTimingGroup);
                        if (errorType != AffErrorType.none)
                            return getError(errorType);
                        break;
                    case "camera":
                        errorType = AddCamera(lineParser);
                        if (errorType != AffErrorType.none)
                            return getError(errorType);
                        break;
                    case "scenecontrol":
                        errorType = AddSceneControlEvent(lineParser);
                        if (errorType != AffErrorType.none)
                            return getError(errorType);
                        break;
                    case "timinggroup":
                        currentTimingGroup++;
                        affTimingList.Add(new List<AffTiming>());
                        break;
                    default:
                        return getError(AffErrorType.invalid_note_type);
                }

                i++;
            }

            Conductor.Instance.SetupTimingGroups(affTimingList);
            BeatlineEntityCreator.Instance.CreateEntities(affTimingList[0]);
            TapEntityCreator.Instance.CreateEntities(affTapList);
            HoldEntityCreator.Instance.CreateEntities(affHoldList);
            ArcEntityCreator.Instance.CreateEntities(affArcList);
            TraceEntityCreator.Instance.CreateEntities(affTraceList);
            ArcTapEntityCreator.Instance.CreateEntities(affArcTapList, affTapList);

            Debug.Log("Finished loading entities");
            Conductor.Instance.PlayMusic();

            GameState.isChartMode = true;

            return null;
        }

        private void SetAudioOffset(int offset)
        {
            Conductor.Instance.offset = offset;
        }

        private AffErrorType NoFoundOr(StringParser.Status status, AffErrorType t)
            => status == StringParser.Status.failure_invalid_terminator ? AffErrorType.no_found_item : t;

        private AffErrorType AddTiming(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_time);

            if (!lineParser.ParseFloat(out float bpm, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseFloat(out float divisor, ")"))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            affTimingList[currentTimingGroup].Add(new AffTiming() { timing = timing, bpm = bpm, divisor = divisor });
            return AffErrorType.none;
        }

        private AffErrorType AddTap(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_time);

            if (!lineParser.ParseInt(out int track, ")"))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_lane);

            if (track < 1 || track > 4)
                return AffErrorType.invalid_lane;

            affTapList.Add(new AffTap() { timing = timing, track = track, timingGroup = currentTimingGroup });
            return AffErrorType.none;
        }

        private AffErrorType AddHold(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_time);

            if (!lineParser.ParseInt(out int endTiming, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_time);

            if (!lineParser.ParseInt(out int track, ")"))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_lane);

            if (track < 1 || track > 4)
                return AffErrorType.invalid_lane;

            affHoldList.Add(new AffHold() { timing = timing, endTiming = endTiming, track = track, timingGroup = currentTimingGroup });
            return AffErrorType.none;
        }

        private AffErrorType AddArc(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_time);

            if (!lineParser.ParseInt(out int endTiming, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_time);

            if (!lineParser.ParseFloat(out float startX, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseFloat(out float endX, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ReadString(out string vl, ","))
                return AffErrorType.no_found_item;

            if (!GetEasingType(out ArcEasing easing, vl))
                return AffErrorType.improper_arctype;

            if (!lineParser.ParseFloat(out float startY, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseFloat(out float endY, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseInt(out int color, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_int);

            lineParser.SkipPast(",");

            if (!lineParser.ParseBool(out bool isTrace, ")"))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_boolean);

            if (!isTrace)
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
                        return AffErrorType.no_found_item;

                    float p = (float) (t - timing) / (endTiming - timing);

                    float x = Conversion.GetXAt(p, startX, endX, easing);
                    float y = Conversion.GetYAt(p, startY, endY, easing);

                    affArcTapList.Add(new AffArcTap()
                    {
                        timing = t,
                        position = new float2(x, y),
                        timingGroup = currentTimingGroup
                    });
//wwwwww
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
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_time);

            if (!lineParser.ParseFloat(out float xpos, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseFloat(out float ypos, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseFloat(out float zpos, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            //Arcade's coordinate system seems to be different to arcaea's

            if (!lineParser.ParseFloat(out float yrot, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseFloat(out float xrot, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ParseFloat(out float zrot, ","))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_float);

            if (!lineParser.ReadString(out string ce, ","))
                return AffErrorType.no_found_item;

            if (!GetCameraEasing(out CameraEasing easing, ce))
                return AffErrorType.improper_camtype;

            if (!lineParser.ParseInt(out int duration, ")"))
                return NoFoundOr(lineParser.LastStatus, AffErrorType.improper_int);

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
}