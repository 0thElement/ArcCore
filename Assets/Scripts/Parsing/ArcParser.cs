using System.Collections.Generic;

namespace ArcCore.Parsing
{
    using ArcCore.Gameplay.Behaviours;
    using ArcCore.Storage;
    using ArcCore.Utilities;
    using Data;
    using System.IO;
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
        public List<List<TimingRaw>> Timings { get; private set; } = new List<List<TimingRaw>> { new List<TimingRaw>() };
        public List<TapRaw> Taps { get; private set; } = new List<TapRaw>();
        public List<HoldRaw> Holds { get; private set; } = new List<HoldRaw>();
        public List<ArcRaw> Arcs { get; private set; } = new List<ArcRaw>();
        public List<ArcRaw> Traces { get; private set; } = new List<ArcRaw>();
        public List<ArctapRaw> Arctaps { get; private set; } = new List<ArctapRaw>();
        public List<CameraEvent> Cameras { get; private set; } = new List<CameraEvent>();
        public List<int> CameraResets { get; private set; } = new List<int>();
        public int ChartOffset { get; private set; }
        public List<TimingGroupFlag> TimingGroupFlags { get; private set; } = new List<TimingGroupFlag>();
        public int MaxArcColor { get; set; }
        public List<(ScenecontrolData, TextScenecontrolData)> TextScenecontrolData { get; private set; } 
            = new List<(ScenecontrolData, TextScenecontrolData)>();
        public List<(ScenecontrolData, SpriteScenecontrolData)> SpriteScenecontrolData { get; private set; }
            = new List<(ScenecontrolData, SpriteScenecontrolData)>();

        public ArcParser(string[] lines): base(lines) {}
        public ArcParser(CommandParser parser): base(parser) {}

        private int timingGroup;
        private bool timingGroupHasTiming;
        private bool timingGroupHasTrace;
        private ArcParserState state;

        private Dictionary<string, ScenecontrolData> masterSCDict = new Dictionary<string, ScenecontrolData>();
        private Dictionary<string, TextScenecontrolData> textSCDict = new Dictionary<string, TextScenecontrolData>();
        private Dictionary<string, SpriteScenecontrolData> spriteSCDict = new Dictionary<string, SpriteScenecontrolData>();

        private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

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

        private bool TryGetCameraEasing(string value, out CameraEasing result)
        {
            switch(value)
            {
                case "l":
                    result = CameraEasing.l;
                    return true;
                case "qi":
                    result = CameraEasing.qi;
                    return true;
                case "qo":
                    result = CameraEasing.qo;
                    return true;
                default:
                    result = CameraEasing.l;
                    return false;
            }
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

        private void NewTextObject()
        {
            var iden = GetValue("identifier");
            var text = GetStrValue();

            masterSCDict.Add(iden, new ScenecontrolData());
            textSCDict.Add(iden, new TextScenecontrolData(text));
        }
        private void NewSpriteObject()
        {
            var iden = GetValue("identifier");
            var spr = GetValue("identifier");

            if(!sprites.ContainsKey(spr))
                throw GetParsingException($"Undeclared sprite cannot be used: \"{spr}\".");

            masterSCDict.Add(iden, new ScenecontrolData());
            spriteSCDict.Add(iden, new SpriteScenecontrolData(sprites[spr]));
        }

        private protected override Command ExecuteCommand(string command)
        {
            //message and predicate constants
            bool settingsMode = (state == ArcParserState.Settings);
            const string SettingsModeMsg = "Cannot use this command unless in settings mode.";

            bool objectsMode = (state == ArcParserState.Objects);
            const string ObjectsModeMsg = "Cannot use this command unless in objects mode.";

            bool chartMode = (state == ArcParserState.Chart);
            const string ChartModeMsg = "Cannot use this command unless in chart mode.";

            bool chartObjMode = (state == ArcParserState.Chart && timingGroupHasTiming);
            const string ChartObjModeMsg = "Cannot create this chart item unless in chart mode and timing group has at least one timing.";

            bool arctap = chartObjMode && timingGroupHasTrace;
            const string ArctapMsg = "Cannot create this chart item unless in chart mode, timing group has at least one timing, and there is at least one trace in the current timinggroup.";

            //actual cases
            switch (command)
            {
                //mode commands
                case "settings":
                    return (
                        "settings header", 
                        () => 
                        {
                            ClearContext();
                            AddPermanentContext("settings");
                            state = ArcParserState.Settings;
                        }, 
                        state < ArcParserState.Settings, 
                        "Cannot use a settings header more than once or after a chart or objects header."
                    );

                case "objects":
                    return (
                        "objects header",
                        () =>
                        {
                            ClearContext();
                            AddPermanentContext("objects");
                            state = ArcParserState.Objects;
                        },
                        state < ArcParserState.Objects,
                        "Cannot use an objects header more than once or after a chart header."
                    );

                case "chart":
                    return (
                        "chart header",
                        () =>
                        {
                            ClearContext();
                            AddPermanentContext("chart");
                            state = ArcParserState.Chart;
                        },
                        state < ArcParserState.Chart,
                        "Cannot use a chart header more than once."
                    );

                //settings commands
                case "audio_offset":
                    return ("audio offset setter", () => ChartOffset = GetInt(), settingsMode, SettingsModeMsg);

                //objects commands
                case "image":
                    return ("image creator", NewImage, objectsMode, ObjectsModeMsg);
                case "text":
                    return ("text creator", NewTextObject, objectsMode, ObjectsModeMsg);
                case "sprite":
                    return ("sprite creator", NewSpriteObject, objectsMode, ObjectsModeMsg);

                //chart commands
                case "timing":
                    return ("timing", NewTiming, chartMode, ChartModeMsg);
                case "tap":
                    return ("tap", NewTap, chartObjMode, ChartObjModeMsg);
                case "hold":
                    return ("hold", NewHold, chartObjMode, ChartObjModeMsg);
                case "arc":
                    return ("arc", NewArc, chartObjMode, ChartObjModeMsg);
                case "trace":
                    return ("trace", NewTrace, chartObjMode, ChartObjModeMsg);
                case "arctap":
                    return ("arctap", NewArctap, arctap, ArctapMsg);
                case "camera":
                    return ("camera", NewCamera, chartObjMode, ChartObjModeMsg);
                case "cam_reset":
                    return ("camera reset", NewCameraReset, chartObjMode, ChartObjModeMsg);
                case "keyframe":
                    return ("keyframe", NewKeyframe, chartObjMode, ChartObjModeMsg);
                case "new_group":
                    return ("new timinggroup", ManageGroup, chartObjMode, ChartObjModeMsg);
            }

            return null;
        }

        private protected override void OnExecutionEnd()
        {
            foreach (var kvp in textSCDict)
                TextScenecontrolData.Add((masterSCDict[kvp.Key], kvp.Value));

            foreach (var kvp in spriteSCDict)
                SpriteScenecontrolData.Add((masterSCDict[kvp.Key], kvp.Value));
        }

        private int GetLane()
            => GetInt("lane", value => value >= 0 && value <= 5);

        private void NewImage()
        {
            var name = GetValue("identifier");
            var opath = GetValue("path");
            //WARNING: THIS IS A RELATIVE PATH
            //FUCKING FIX THIS SHIT PLEASE
            var path = FileStorage.GetFilePath(opath);

            Texture2D tex = new Texture2D(2,2);

            if (File.Exists(path))
            {
                var data = File.ReadAllBytes(path);
                if (!tex.LoadImage(data)) goto ERR;
            }
            else goto ERR;

            sprites[name] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2);
            return;

            ERR:
            throw GetParsingException($"The provided path did not exist: \"{opath}\".");
        }
        private void NewKeyframe()
        {
            EasingType GetEasing() => GetTypedValue<EasingType>("general easing type", Easing.TryParse);

            var obj = GetValue("identifier");
            var isSprite = spriteSCDict.ContainsKey(obj);

            if (!isSprite && !textSCDict.ContainsKey(obj))
                throw GetParsingException($"Undeclared object: \"{obj}\".");

            var type = GetValue("keyframe type");
            var timing = GetInt();

            void AppendNewAxis(IndexedList<ControlAxisKey> l) => l.internalList.Add(new ControlAxisKey { timing = timing, easing = GetEasing(), targetValue = GetFloat() });
            void RequireSprite()
            {
                if (!isSprite) throw GetParsingException($"Non-sprite object \"{obj}\" cannot have property \"{type}\" set.");
            }
            void RequireText()
            {
                if (isSprite) throw GetParsingException($"Non-text object \"{obj}\" cannot have property \"{type}\" set.");
            }

            switch (type)
            {
                case "x_pos":
                    AppendNewAxis(masterSCDict[obj].xAxisKeys);
                    break;
                case "y_pos":
                    AppendNewAxis(masterSCDict[obj].yAxisKeys);
                    break;
                case "z_pos":
                    AppendNewAxis(masterSCDict[obj].zAxisKeys);
                    break;
                case "x_rot":
                    AppendNewAxis(masterSCDict[obj].xRotAxisKeys);
                    break;
                case "y_rot":
                    AppendNewAxis(masterSCDict[obj].yRotAxisKeys);
                    break;
                case "z_rot":
                    AppendNewAxis(masterSCDict[obj].zRotAxisKeys);
                    break;
                case "x_scl":
                    AppendNewAxis(masterSCDict[obj].xScaleAxisKeys);
                    break;
                case "y_scl":
                    AppendNewAxis(masterSCDict[obj].yScaleAxisKeys);
                    break;
                case "z_scl":
                    AppendNewAxis(masterSCDict[obj].zScaleAxisKeys);
                    break;
                case "red":
                    AppendNewAxis(masterSCDict[obj].redKeys);
                    break;
                case "green":
                    AppendNewAxis(masterSCDict[obj].greenKeys);
                    break;
                case "blue":
                    AppendNewAxis(masterSCDict[obj].blueKeys);
                    break;
                case "alpha":
                    AppendNewAxis(masterSCDict[obj].alphaKeys);
                    break;
                case "enable":
                    masterSCDict[obj].enableKeys.internalList.Add(new ControlValueKey<bool> { timing = timing, value = GetBool() });
                    break;

                case "image":

                    RequireSprite();

                    var spr = GetValue("identifier");
                    if (!sprites.ContainsKey(spr))
                        throw GetParsingException($"Undeclared sprite cannot be used: \"{spr}\".");

                    spriteSCDict[obj].imageKeys.internalList.Add(new ControlValueKey<Sprite> { timing = timing, value = sprites[spr] });
                    break;

                case "sortLayer":

                    RequireSprite();
                    spriteSCDict[obj].sortLayerKeys.internalList.Add(new ControlValueKey<int> { timing = timing, value = GetInt() });
                    break;

                case "string":

                    RequireText();
                    textSCDict[obj].stringKeys.internalList.Add(new ControlValueKey<string> { timing = timing, value = GetStrValue() });
                    break;

                default:
                    throw GetParsingException($"Invalid keyframe type: {type}.");
            }
        }
        private void NewTiming()
        {
            var raw = new TimingRaw();

            raw.timing = GetInt();
            raw.bpm = GetFloat();
            raw.divisor = GetFloat();

            Timings[timingGroup].Add(raw);

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
            int t, col;
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
                    color = col = GetInt(predicate: val => val >= 0),
                    timingGroup = timingGroup
                }
            );

            if (MaxArcColor < col)
                MaxArcColor = col;
        }
        private void NewTrace()
        {
            int t;
            Traces.Add(
                new ArcRaw
                {
                    timing = t = GetInt(),
                    endTiming = GetInt(predicate: val => val >= t),
                    startX = GetFloat(),
                    startY = GetFloat(),
                    endX = GetFloat(),
                    endY = GetFloat(),
                    easing = GetTypedValue<ArcEasing>("arc easing", TryGetArcEasing),
                    timingGroup = timingGroup,
                    color = -1
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
                    Duration = GetInt(),
                    PosChangeFromParam = new float3(GetFloat(), GetFloat(), GetFloat()), 
                    RotChangeFromParam = new float3(GetFloat(), GetFloat(), GetFloat()),
                    easing = GetTypedValue<EasingType>("camera easing type", Easing.TryParse)
                }
            );
        }
        private void NewCameraReset()
        {
            CameraResets.Add(GetInt());
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

            Timings.Add(new List<TimingRaw>());
        }
    }
}
