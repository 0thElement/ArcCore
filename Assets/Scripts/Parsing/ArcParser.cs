using System;
using System.Collections.Generic;

namespace ArcCore.Parsing
{
    using ArcCore.Gameplay;
    using ArcCore.Gameplay.Behaviours;
    using ArcCore.Math;
    using ArcCore.Utilities;
    using Data;
    using System.IO;
    using System.Text;
    using Unity.Mathematics;
    using UnityEngine;

    public enum ArcParserState
    {
        None,
        Settings,
        Objects,
        Chart
    }

    public class ArcParser : CommandParser, IChartParser
    {
        public List<TimingRaw> Timings { get; private set; } = new List<TimingRaw>();
        public List<TapRaw> Taps { get; private set; } = new List<TapRaw>();
        public List<HoldRaw> Holds { get; private set; } = new List<HoldRaw>();
        public List<ArcRaw> Arcs { get; private set; } = new List<ArcRaw>();
        public List<TraceRaw> Traces { get; private set; } = new List<TraceRaw>();
        public List<ArctapRaw> Arctaps { get; private set; } = new List<ArctapRaw>();
        public List<CameraEvent> Cameras { get; private set; } = new List<CameraEvent>();
        public int ChartOffset { get; private set; }
        public List<TimingGroupFlag> TimingGroupFlags { get; private set; } = new List<TimingGroupFlag>();
        public List<int> UsedArcColors { get; private set; } = new List<int>();
        public List<BaseSCObject> SceneControlObjects { get; private set; } = new List<BaseSCObject>();

        public ArcParser(string[] lines): base(lines) {}

        private int timingGroup;
        private bool timingGroupHasTiming;
        private bool timingGroupHasTrace;
        private ArcParserState state;
        private Dictionary<string, Sprite> textureObjects = new Dictionary<string, Sprite>();
        private Dictionary<string, SpriteSCObject> spriteSCObjects = new Dictionary<string, SpriteSCObject>();
        private Dictionary<string, TextSCObject> textSCObjects = new Dictionary<string, TextSCObject>();

        private bool TryGetTiminggroupFlag(string value, out TimingGroupFlag result)
        {
            switch(value)
            {
                case "no_input":
                    result = TimingGroupFlag.NoInput;
                    break;
                default:
                    result = TimingGroupFlag.None;
                    return false;
            }

            return true;
        }

        private bool TryGetArcEasing(string value, out ArcEasing result)
        {
            switch(value)
            {
                case "s":
                    result = ArcEasing.s;
                    break;
                case "b":
                    result = ArcEasing.b;
                    break;
                case "si":
                    result = ArcEasing.si;
                    break;
                case "so":
                    result = ArcEasing.so;
                    break;
                case "sisi":
                    result = ArcEasing.sisi;
                    break;
                case "siso":
                    result = ArcEasing.siso;
                    break;
                case "sosi":
                    result = ArcEasing.sosi;
                    break;
                case "soso":
                    result = ArcEasing.soso;
                    break;
                default:
                    result = ArcEasing.s;
                    return false;
            }
            return true;
        }

        private protected override CommandData GetCommandData(string command)
        {
            //message and predicate constants
            const string SettingsModeMsg = "Cannot use this command unless in settings mode.";
            bool SettingsMode() => (state == ArcParserState.Settings);

            const string ObjectsModeMsg = "Cannot use this command unless in objects mode.";
            bool ObjectsMode() => (state == ArcParserState.Objects);

            const string ChartModeMsg = "Cannot use this command unless in chart mode.";
            bool ChartMode() => (state == ArcParserState.Chart);

            const string ChartObjMsg = "Cannot create this chart item unless in chart mode and timing group has at least one timing.";
            bool ChartObjMode() => (state == ArcParserState.Chart && timingGroupHasTiming);

            const string ArctapMsg = "Cannot create this chart item unless in chart mode, timing group has at least one timing, and there is at least one trace in the current timinggroup.";
            bool Arctap() => ChartObjMode() && timingGroupHasTrace;

            //short methods
            void SetOffset() => ChartOffset = GetTypedValue<int>("integer", int.TryParse);
            void NewTextObject() => textSCObjects.Add(GetValue("identifier"), new TextSCObject { startValue = GetStrValue() });
            void NewSpriteObject() => spriteSCObjects.Add(GetValue("identifier"), new SpriteSCObject { startSprite = textureObjects[GetValue("identifier")] });

            //actual cases
            switch (command)
            {
                //mode commands
                case "settings":
                    return ("settings header", () => state = ArcParserState.Settings, 
                        "Cannot use a settings header more than once or after a chart or objects header.", () => state < ArcParserState.Settings);
                case "objects":
                    return ("objects header", () => state = ArcParserState.Objects,
                        "Cannot use an objects header more than once or after a chart header.", () => state < ArcParserState.Objects);
                case "chart":
                    return ("chart header", () => state = ArcParserState.Chart,
                        "Cannot use a chart header more than once.", () => state < ArcParserState.Chart);

                //settings commands
                case "audio_offset":
                    return ("audio offset setter", SetOffset, SettingsModeMsg, SettingsMode);

                //objects commands
                case "text":
                    return ("text object", NewTextObject, ObjectsModeMsg, ObjectsMode);
                case "sprite":
                    return ("sprite object", NewSpriteObject, ObjectsModeMsg, ObjectsMode);

                //chart commands
                case "timing":
                    return ("", NewTiming, ChartModeMsg, ChartMode);
                case "tap":
                    return ("", NewTap, ChartObjMsg, ChartObjMode);
                case "hold":
                    return ("", NewHold, ChartObjMsg, ChartObjMode);
                case "arc":
                    return ("", NewArc, ChartObjMsg, ChartObjMode);
                case "trace":
                    return ("", NewTrace, ChartObjMsg, ChartObjMode);
                case "arctap":
                    return ("", NewArctap, ArctapMsg, Arctap);
                case "camera":
                    return ("", NewCamera, ChartObjMsg, ChartObjMode);
                case "new_group":
                    return ("new timing group", ManageGroup, ChartModeMsg, ChartMode);
            }

            return null;
        }

        private int GetLane()
            => GetInt("lane", value => 0 < value && value < 5);

        private void NewTiming()
        {
            Timings.Add(
                new TimingRaw
                {
                    timing = GetInt(),
                    bpm = GetFloat(),
                    divisor = GetFloat()
                }
            );
            timingGroupHasTiming = true;
        }
        private void NewTap()
        {
            Taps.Add(
                new TapRaw
                {
                    timing = GetInt(),
                    track = GetLane(),
                    timingGroup = timingGroup
                }
            );
        }
        private void NewHold()
        {
            int t;
            Holds.Add(
                new HoldRaw
                {
                    timing = t = GetInt(),
                    endTiming = GetInt(predicate: val => val >= t),
                    track = GetLane(),
                    timingGroup = timingGroup
                }
            );
        }
        private void NewArc()
        {
            int t;
            Arcs.Add(
                new ArcRaw
                {
                    timing = t = GetInt(),
                    endTiming = GetInt(predicate: val => val >= t),
                    startX = GetFloat(),
                    startY = GetFloat(),
                    endX = GetFloat(),
                    endY = GetFloat(),
                    easing = GetTypedValue<ArcEasing>("arc easing", TryGetArcEasing),
                    color = GetInt(predicate: val => val > 0),
                    timingGroup = timingGroup
                }
            );
        }
        private void NewTrace()
        {
            int t;
            Traces.Add(
                new TraceRaw
                {
                    timing = t = GetInt(),
                    endTiming = GetInt(predicate: val => val >= t),
                    startX = GetFloat(),
                    startY = GetFloat(),
                    endX = GetFloat(),
                    endY = GetFloat(),
                    easing = GetTypedValue<ArcEasing>("arc easing", TryGetArcEasing),
                    timingGroup = timingGroup
                }
            );

            timingGroupHasTrace = true;
        }
        private void NewArctap()
        {
            int t;
            Arctaps.Add(
                new ArctapRaw
                {
                    timing = t = GetInt(),
                    position = Traces[Traces.Count - 1].GetPosAt(t),
                    timingGroup = timingGroup
                }
            );
        }
        private void NewCamera()
        {
            Cameras.Add(
                new CameraEvent
                {
                    Timing = GetInt(),
                    targetChange = new PosRot(new float3(GetFloat(), GetFloat(), GetFloat()), new float3(GetFloat(), GetFloat(), GetFloat()))
                }
            );
        }

        private void ManageGroup()
        {
            timingGroup++;
            TimingGroupFlag flag = TimingGroupFlag.None;

            while(GetTypedValue<TimingGroupFlag>(out var value, "timing group flag", TryGetTiminggroupFlag))
            {
                flag |= value;
            }

            TimingGroupFlags.Add(flag);

            timingGroupHasTiming = false;
            timingGroupHasTrace = false;
        }
    }
}
