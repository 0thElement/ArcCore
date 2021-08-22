using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Parsing.Data;
using System.Text.RegularExpressions;
using ArcCore.Utilities;
using ArcCore.Parsing;
/*
namespace ArcCore.Gameplay.Behaviours
{
    public class ChartReader : MonoBehaviour, IChartParser
    {
        public static ChartReader Instance { get; private set; }

        public List<TimingRaw> Timings => throw new System.NotImplementedException();

        public List<TapRaw> Taps => throw new System.NotImplementedException();

        public List<HoldRaw> Holds => throw new System.NotImplementedException();

        public List<ArcRaw> Arcs => throw new System.NotImplementedException();

        public List<TraceRaw> Traces => throw new System.NotImplementedException();

        public List<ArctapRaw> Arctaps => throw new System.NotImplementedException();

        public List<CameraEvent> Cameras => throw new System.NotImplementedException();

        public List<int> CameraResets => throw new System.NotImplementedException();

        public int ChartOffset => throw new System.NotImplementedException();

        public List<TimingGroupFlag> TimingGroupFlags => throw new System.NotImplementedException();

        public HashSet<int> UsedArcColors => throw new System.NotImplementedException();

        public List<(ScenecontrolData, TextScenecontrolData)> TextScenecontrolData => throw new System.NotImplementedException();

        public List<(ScenecontrolData, SpriteScenecontrolData)> SpriteScenecontrolData => throw new System.NotImplementedException();

        private string path;

        private List<List<TimingRaw>> affTimingList = new List<List<TimingRaw>>();
        private List<TapRaw> affTapList = new List<TapRaw>();
        private List<HoldRaw> affHoldList = new List<HoldRaw>();
        private List<List<ArcRaw>> affArcList = new List<List<ArcRaw>>();
        private List<TraceRaw> affTraceList = new List<TraceRaw>();
        private List<ArctapRaw> affChartTapList = new List<ArctapRaw>();
        private List<CameraEvent> affCameraList = new List<CameraEvent>();
        private List<ChartSceneControlEvent> affSceneControlEventList = new List<ChartSceneControlEvent>();
        private List<int> affResetTimingsList = new List<int>();

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 200;
        }

        private IEnumerator DebugCoroutine()
        {
            yield return null;
            ChartError err;
            if ((err = ReadChart(Constants.GetCamDebugChart())) != null)
                Debug.LogError(err);
        }

        private void Start()
        {
            StartCoroutine(DebugCoroutine());
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

        private ChartError ReadChartFromFile(string path)
        {
            return ReadChart(File.ReadAllText(path));
        }
        private ChartError ReadChart(string data)
        {
            string[] lines = Regex.Split(data, "\n\r|\r\n|\r|\n");

            for(int l = 0; l < lines.Length; l++)
            {
                lines[l] = lines[l].Trim();
            }

            //Read all header options
            int i = 0;

            //Local funcs cuz f*ck this
            ChartError getError(ChartErrorType t)
                => new ChartError(t, i + 1);

            while (i < lines.Length && lines[i][0] != '-')
            {
                if (lines[i].IndexOf(":") != -1)
                {
                    StringParser lineParser = new StringParser(lines[i]);

                    if (!lineParser.ReadString(out string option, ":"))
                        return getError(ChartErrorType.no_found_item);

                    switch (option)
                    {
                        case "AudioOffset":
                            if (!lineParser.ParseInt(out int audioOff))
                                return getError(ChartErrorType.invalid_audio_offset);
                            SetAudioOffset(audioOff);
                            break;
                        default:
                            return getError(ChartErrorType.invalid_header_option);
                    }
                }
                i++;
            }

            //Read notes
            i++;
            int currentTimingGroup = 0;
            affTimingList.Add(new List<TimingRaw>());

            while (i < lines.Length)
            {
                if (lines[i][0] == '}' || lines[i][0] == '{')
                {
                    i++;
                    continue;
                }

                StringParser lineParser = new StringParser(lines[i]);

                if (!lineParser.ReadString(out string type, "("))
                    return getError(ChartErrorType.invalid_line_format);

                ChartErrorType errorType;

                switch (type)
                {
                    case "timing":
                        errorType = AddTiming(lineParser, currentTimingGroup);
                        if (errorType != ChartErrorType.none)
                            return getError(errorType);
                        break;
                    case "":
                        errorType = AddTap(lineParser, currentTimingGroup);
                        if (errorType != ChartErrorType.none)
                            return getError(errorType);
                        break;
                    case "hold":
                        errorType = AddHold(lineParser, currentTimingGroup);
                        if (errorType != ChartErrorType.none)
                            return getError(errorType);
                        break;
                    case "arc":
                        errorType = AddArc(lineParser, currentTimingGroup);
                        if (errorType != ChartErrorType.none)
                            return getError(errorType);
                        break;
                    case "camera":
                        errorType = AddCamera(lineParser);
                        if (errorType != ChartErrorType.none)
                            return getError(errorType);
                        break;
                    case "scenecontrol":
                        errorType = AddSceneControlEvent(lineParser);
                        if (errorType != ChartErrorType.none)
                            return getError(errorType);
                        break;
                    case "timinggroup":
                        currentTimingGroup++;
                        affTimingList.Add(new List<TimingRaw>());
                        break;
                    default:
                        return getError(ChartErrorType.invalid_note_type);
                }

                i++;
            }

            PlayManager.Conductor.SetupTimingGroups(affTimingList);
            BeatlineEntityCreator.Instance.CreateEntities(affTimingList[0]);
            TapEntityCreator.Instance.CreateEntities(affTapList, affArcTapList);
            HoldEntityCreator.Instance.CreateEntities(affHoldList);
            ArcEntityCreator.Instance.CreateEntities(affArcList);
            TraceEntityCreator.Instance.CreateEntities(affTraceList);
            ArcTapEntityCreator.Instance.CreateEntities(affChartTapList, affTapList);

            affCameraList.Sort((c1, c2) => c1.Timing.CompareTo(c2.Timing));
            GameplayCamera.Instance.cameraMovements = affCameraList.ToArray();
            GameplayCamera.Instance.resetTimings = affResetTimingsList.ToArray();

            Debug.Log("Finished loading entities");
            PlayManager.Conductor.PlayMusic();

            GameState.isChartMode = true;

            return null;
        }

        private void SetAudioOffset(int offset)
        {
            PlayManager.Conductor.chartOffset = offset;
        }

        private ChartErrorType NoFoundOr(StringParser.Status status, ChartErrorType t)
            => status == StringParser.Status.failure_invalid_terminator ? ChartErrorType.no_found_item : t;

        private ChartErrorType AddTiming(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_time);

            if (!lineParser.ParseFloat(out float bpm, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float divisor, ")"))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            affTimingList[currentTimingGroup].Add(new TimingRaw() { timing = timing, bpm = bpm, divisor = divisor });
            return ChartErrorType.none;
        }

        private ChartErrorType AddTap(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_time);

            if (!lineParser.ParseInt(out int track, ")"))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_lane);

            if (track < 1 || track > 4)
                return ChartErrorType.invalid_lane;

            affTapList.Add(new TapRaw() { timing = timing, track = track, timingGroup = currentTimingGroup });
            return ChartErrorType.none;
        }

        private ChartErrorType AddHold(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_time);

            if (!lineParser.ParseInt(out int endTiming, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_time);

            if (!lineParser.ParseInt(out int track, ")"))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_lane);

            if (track < 1 || track > 4)
                return ChartErrorType.invalid_lane;

            affHoldList.Add(new HoldRaw() { timing = timing, endTiming = endTiming, track = track, timingGroup = currentTimingGroup });
            return ChartErrorType.none;
        }

        private ChartErrorType AddArc(StringParser lineParser, int currentTimingGroup)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_time);

            if (!lineParser.ParseInt(out int endTiming, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_time);

            if (!lineParser.ParseFloat(out float startX, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float endX, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ReadString(out string vl, ","))
                return ChartErrorType.no_found_item;

            if (!GetEasingType(out ArcEasing easing, vl))
                return ChartErrorType.improper_arctype;

            if (!lineParser.ParseFloat(out float startY, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float endY, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseInt(out int color, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_int);

            lineParser.SkipPast(",");

            if (!lineParser.ParseBool(out bool isTrace, ")"))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_boolean);

            if (!isTrace)
            {
                while (affArcList.Count < color + 1) affArcList.Add(new List<ArcRaw>());
                affArcList[color].Add(new ArcRaw()
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
            else affTraceList.Add(new TraceRaw()
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

            if (lineParser.Current != ";")
            {
                //parse arctap
                do
                {
                    lineParser.Skip(8);
                    if (!lineParser.ParseInt(out int t, ")"))
                        return ChartErrorType.no_found_item;

                    float p = (float) (t - timing) / (endTiming - timing);

                    float x = Conversion.GetXAt(p, startX, endX, easing);
                    float y = Conversion.GetYAt(p, startY, endY, easing);

                    affChartTapList.Add(new ArctapRaw()
                    {
                        timing = t,
                        position = new float2(x, y),
                        timingGroup = currentTimingGroup
                    });
//wwwwww
                } while (lineParser.Current == ",");
            }

            return ChartErrorType.none;
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

        private ChartErrorType AddCamera(StringParser lineParser)
        {
            if (!lineParser.ParseInt(out int timing, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_time);

            if (!lineParser.ParseFloat(out float xpos, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float ypos, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float zpos, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float xrot, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float yrot, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ParseFloat(out float zrot, ","))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_float);

            if (!lineParser.ReadString(out string ce, ","))
                return ChartErrorType.no_found_item;

            if (!GetCameraEasing(out CameraEasing easing, out var isReset, ce))
                return ChartErrorType.improper_camtype;

            if (!lineParser.ParseInt(out int duration, ")"))
                return NoFoundOr(lineParser.LastStatus, ChartErrorType.improper_int);

            if (isReset)
            {
                affResetTimingsList.Add(timing);
            }
            else
            {
                affCameraList.Add(new CameraEvent
                {
                    Timing = timing,
                    PosChangeFromParam = new float3(xpos, ypos, zpos),
                    RotChangeFromParam = new float3(xrot, yrot, zrot),
                    easing = Easing.FromCameraEase(easing),
                    Duration = duration
                });
            }

            return ChartErrorType.none;
        }

        private static bool GetCameraEasing(out CameraEasing easing, out bool isReset, string easingString)
        {
            isReset = false;
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
                    easing = CameraEasing.l;
                    isReset = true;
                    break;
                default:
                    easing = CameraEasing.l;
                    return false;
            }

            return true;
        }

        public ChartErrorType AddSceneControlEvent(StringParser lineParser)
        {
            //todo: scenecontrol support, maybe 
            return ChartErrorType.none;
        }

        public void Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}
*/